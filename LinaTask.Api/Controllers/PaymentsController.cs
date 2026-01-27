using LinaTask.Application.DTOs;
using LinaTask.Application.Services;
using LinaTask.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los pagos
        /// </summary>
        [HttpGet("getAllPayments")]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAllPayments()
        {
            try
            {
                var payments = await _paymentService.GetAllPaymentsAsync();
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pagos");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene un pago por ID
        /// </summary>
        [HttpGet("getPaymentById/{id:guid}")]
        public async Task<ActionResult<PaymentDto>> GetPaymentById(Guid id)
        {
            try
            {
                var payment = await _paymentService.GetPaymentByIdAsync(id);
                if (payment == null)
                    return NotFound($"Pago con ID {id} no encontrado");

                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pago {PaymentId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene pagos por estudiante
        /// </summary>
        [HttpGet("getPaymentsByStudentId/{studentId:guid}")]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPaymentsByStudentId(Guid studentId)
        {
            try
            {
                var payments = await _paymentService.GetPaymentsByStudentAsync(studentId);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pagos del estudiante");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene pagos por tarea
        /// </summary>
        [HttpGet("getPaymentsByTaskId/{taskId:guid}")]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPaymentsByTaskId(Guid taskId)
        {
            try
            {
                var payments = await _paymentService.GetPaymentsByTaskAsync(taskId);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pagos de la tarea");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene pagos por estado
        /// </summary>
        [HttpGet("getPaymentsByStatus/{status}")]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPaymentsByStatus(string status)
        {
            try
            {
                var payments = await _paymentService.GetPaymentsByStatusAsync(status);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pagos por estado");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene el total gastado por un estudiante
        /// </summary>
        [HttpGet("getTotalSpentByStudentId/{studentId:guid}")]
        public async Task<ActionResult<decimal>> GetTotalSpentByStudentId(Guid studentId)
        {
            try
            {
                var total = await _paymentService.GetTotalSpentByStudentAsync(studentId);
                return Ok(new { studentId, totalSpent = total });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener total del estudiante");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene estadísticas de la plataforma
        /// </summary>
        [HttpGet("getPlatformStats")]
        public async Task<ActionResult<PaymentStatsDto>> GetPlatformStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var stats = await _paymentService.GetPlatformStatsAsync(startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Crea un nuevo pago
        /// </summary>
        [HttpPost("createPayment")]
        public async Task<ActionResult<PaymentDto>> CreatePayment([FromBody] CreatePaymentDto createPaymentDto)
        {
            try
            {
                var payment = await _paymentService.CreatePaymentAsync(createPaymentDto);
                return CreatedAtAction(nameof(GetPaymentById), new { id = payment.Id }, payment);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear pago");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Actualiza un pago (principalmente el estado)
        /// </summary>
        [HttpPut("updatePayment/{id:guid}")]
        public async Task<ActionResult<PaymentDto>> UpdatePayment(Guid id, [FromBody] UpdatePaymentDto updatePaymentDto)
        {
            try
            {
                var payment = await _paymentService.UpdatePaymentAsync(id, updatePaymentDto);
                return Ok(payment);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Pago con ID {id} no encontrado");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar pago {PaymentId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Elimina un pago
        /// </summary>
        [HttpDelete("deletePayment/{id:guid}")]
        public async Task<IActionResult> DeletePayment(Guid id)
        {
            try
            {
                var deleted = await _paymentService.DeletePaymentAsync(id);
                if (!deleted)
                    return NotFound($"Pago con ID {id} no encontrado");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar pago {PaymentId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}