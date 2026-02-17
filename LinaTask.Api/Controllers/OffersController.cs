using LinaTask.Api.Attributes;
using LinaTask.Application.DTOs;
using LinaTask.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere autenticación para todo el controlador
    public class OffersController : ControllerBase
    {
        private readonly IOfferService _offerService;
        private readonly ILogger<OffersController> _logger;

        public OffersController(IOfferService offerService, ILogger<OffersController> logger)
        {
            _offerService = offerService;
            _logger = logger;
        }

        [HttpGet("getAllOffers")]
        [PermissionAuthorize("OFFER.VIEW")]
        public async Task<ActionResult<IEnumerable<OfferDto>>> GetAllOffers()
        {
            try
            {
                var offers = await _offerService.GetAllOffersAsync();
                return Ok(offers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ofertas");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getOfferById/{id:guid}")]
        [PermissionAuthorize("OFFER.VIEW")]
        public async Task<ActionResult<OfferDto>> GetOfferById(Guid id)
        {
            try
            {
                var offer = await _offerService.GetOfferByIdAsync(id);
                if (offer == null)
                    return NotFound($"Oferta con ID {id} no encontrada");

                return Ok(offer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener oferta {OfferId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getOffersByTaskId/{taskId:guid}")]
        [PermissionAuthorize("OFFER.VIEW")]
        public async Task<ActionResult<IEnumerable<OfferDto>>> GetOffersByTaskId(Guid taskId)
        {
            try
            {
                var offers = await _offerService.GetOffersByTaskAsync(taskId);
                return Ok(offers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ofertas de la tarea");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getOffersByTeacherId/{teacherId:guid}")]
        [PermissionAuthorize("OFFER.VIEW")]
        public async Task<ActionResult<IEnumerable<OfferDto>>> GetOffersByTeacherId(Guid teacherId)
        {
            try
            {
                var offers = await _offerService.GetOffersByTeacherAsync(teacherId);
                return Ok(offers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ofertas del profesor");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost("createOffer")]
        [PermissionAuthorize("OFFER.CREATE")]
        public async Task<ActionResult<OfferDto>> CreateOffer([FromBody] CreateOfferDto createOfferDto)
        {
            try
            {
                var offer = await _offerService.CreateOfferAsync(createOfferDto);
                return CreatedAtAction(nameof(GetOfferById), new { id = offer.Id }, offer);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear oferta");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("updateOffer/{id:guid}")]
        [PermissionAuthorize("OFFER.EDIT")]
        public async Task<ActionResult<OfferDto>> UpdateOffer(Guid id, [FromBody] UpdateOfferDto updateOfferDto)
        {
            try
            {
                var offer = await _offerService.UpdateOfferAsync(id, updateOfferDto);
                return Ok(offer);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Oferta con ID {id} no encontrada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar oferta {OfferId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpDelete("deleteOffer/{id:guid}")]
        [PermissionAuthorize("OFFER.DELETE")]
        public async Task<IActionResult> DeleteOffer(Guid id)
        {
            try
            {
                var deleted = await _offerService.DeleteOfferAsync(id);
                if (!deleted)
                    return NotFound($"Oferta con ID {id} no encontrada");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar oferta {OfferId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}