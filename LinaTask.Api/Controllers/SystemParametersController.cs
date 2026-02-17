using LinaTask.Api.Attributes;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemParametersController : ControllerBase
    {
        private readonly ISystemParameterService _parameterService;
        private readonly ILogger<SystemParametersController> _logger;

        public SystemParametersController(
            ISystemParameterService parameterService,
            ILogger<SystemParametersController> logger)
        {
            _parameterService = parameterService;
            _logger = logger;
        }

        [HttpGet]
        [PermissionAuthorize("SYSTEM_PARAMETER.VIEW")]
        public async Task<ActionResult<IEnumerable<SystemParameterDto>>> GetAllParameters()
        {
            try
            {
                var parameters = await _parameterService.GetAllParametersAsync();
                return Ok(parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los parámetros");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("active")]
        [AllowAnonymous] // Los parámetros activos son públicos
        public async Task<ActionResult<IEnumerable<SystemParameterDto>>> GetActiveParameters()
        {
            try
            {
                var parameters = await _parameterService.GetActiveParametersAsync();
                return Ok(parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener parámetros activos");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("{id:guid}")]
        [PermissionAuthorize("SYSTEM_PARAMETER.VIEW")]
        public async Task<ActionResult<SystemParameterDto>> GetParameterById(Guid id)
        {
            try
            {
                var parameter = await _parameterService.GetParameterByIdAsync(id);
                if (parameter == null)
                    return NotFound($"Parámetro con ID {id} no encontrado");

                return Ok(parameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener parámetro por ID: {ParameterId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("by-key/{key}")]
        [AllowAnonymous] // Consulta pública por clave
        public async Task<ActionResult<SystemParameterDto>> GetParameterByKey(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    return BadRequest("La clave es requerida");

                var parameter = await _parameterService.GetParameterByKeyAsync(key);
                if (parameter == null)
                    return NotFound($"Parámetro con clave '{key}' no encontrado");

                return Ok(parameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener parámetro por clave: {Key}", key);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("value/{key}")]
        [AllowAnonymous] // Valor público
        public async Task<ActionResult<object>> GetParameterValue(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    return BadRequest("La clave es requerida");

                var value = await _parameterService.GetParameterValueAsync(key);
                if (value == null)
                    return NotFound($"Parámetro con clave '{key}' no encontrado o no está activo");

                return Ok(new { key, value });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener valor del parámetro: {Key}", key);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost]
        [PermissionAuthorize("SYSTEM_PARAMETER.CREATE")]
        public async Task<ActionResult<SystemParameterDto>> CreateParameter([FromBody] CreateSystemParameterDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var parameter = await _parameterService.CreateParameterAsync(createDto);
                return CreatedAtAction(nameof(GetParameterById), new { id = parameter.Id }, parameter);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear parámetro");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("{id:guid}")]
        [PermissionAuthorize("SYSTEM_PARAMETER.EDIT")]
        public async Task<ActionResult<SystemParameterDto>> UpdateParameter(Guid id, [FromBody] UpdateSystemParameterDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var parameter = await _parameterService.UpdateParameterAsync(id, updateDto);
                return Ok(parameter);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar parámetro: {ParameterId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("by-key/{key}")]
        [PermissionAuthorize("SYSTEM_PARAMETER.EDIT")]
        public async Task<ActionResult<SystemParameterDto>> UpdateParameterByKey(string key, [FromBody] UpdateSystemParameterDto updateDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    return BadRequest("La clave es requerida");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var parameter = await _parameterService.UpdateParameterByKeyAsync(key, updateDto);
                return Ok(parameter);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar parámetro por clave: {Key}", key);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpDelete("{id:guid}")]
        [PermissionAuthorize("SYSTEM_PARAMETER.DELETE")]
        public async Task<IActionResult> DeleteParameter(Guid id)
        {
            try
            {
                var deleted = await _parameterService.DeleteParameterAsync(id);
                if (!deleted)
                    return NotFound($"Parámetro con ID {id} no encontrado");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar parámetro: {ParameterId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpDelete("by-key/{key}")]
        [PermissionAuthorize("SYSTEM_PARAMETER.DELETE")]
        public async Task<IActionResult> DeleteParameterByKey(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    return BadRequest("La clave es requerida");

                var deleted = await _parameterService.DeleteParameterByKeyAsync(key);
                if (!deleted)
                    return NotFound($"Parámetro con clave '{key}' no encontrado");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar parámetro por clave: {Key}", key);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost("search")]
        [PermissionAuthorize("SYSTEM_PARAMETER.VIEW")]
        public async Task<ActionResult<IEnumerable<SystemParameterDto>>> SearchParameters([FromBody] SystemParameterSearchDto searchDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var parameters = await _parameterService.SearchParametersAsync(searchDto);
                return Ok(parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar parámetros");
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}