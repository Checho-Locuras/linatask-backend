using LinaTask.Api.Attributes;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("getAllUsers")]
        [PermissionAuthorize("USER.VIEW")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getUserById/{id:guid}")]
        [PermissionAuthorize("USER.VIEW")]
        public async Task<ActionResult<UserDto>> GetUserById(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound($"Usuario con ID {id} no encontrado");

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario {UserId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getUserByEmail/{email}")]
        [PermissionAuthorize("USER.VIEW")]
        public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
        {
            try
            {
                var user = await _userService.GetUserByEmailAsync(email);
                if (user == null)
                    return NotFound($"Usuario con email {email} no encontrado");

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario por email");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost("createUser")]
        [PermissionAuthorize("USER.CREATE")]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                var user = await _userService.CreateUserAsync(createUserDto);
                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("updateUser/{id:guid}")]
        [PermissionAuthorize("USER.EDIT")]
        public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _userService.UpdateUserAsync(id, updateUserDto);
                return Ok(user);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Usuario con ID {id} no encontrado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario {UserId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpDelete("deleteUser/{id:guid}")]
        [PermissionAuthorize("USER.DELETE")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var deleted = await _userService.DeleteUserAsync(id);
                if (!deleted)
                    return NotFound($"Usuario con ID {id} no encontrado");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {UserId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("{userId:guid}/addresses")]
        [PermissionAuthorize("USER.VIEW")]
        public async Task<ActionResult<IEnumerable<UserAddressDto>>> GetUserAddresses(Guid userId)
        {
            try
            {
                var addresses = await _userService.GetUserAddressesAsync(userId);
                return Ok(addresses);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener direcciones del usuario {UserId}", userId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost("{userId:guid}/addresses")]
        [PermissionAuthorize("USER.EDIT")]
        public async Task<ActionResult<UserAddressDto>> AddAddress(
            Guid userId,
            [FromBody] CreateAddressDto createAddressDto)
        {
            try
            {
                var address = await _userService.AddAddressAsync(userId, createAddressDto);
                return CreatedAtAction(
                    nameof(GetUserAddresses),
                    new { userId },
                    address);
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
                _logger.LogError(ex, "Error al agregar dirección al usuario {UserId}", userId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("addresses/{addressId:guid}")]
        [PermissionAuthorize("USER.EDIT")]
        public async Task<ActionResult<UserAddressDto>> UpdateAddress(
            Guid addressId,
            [FromBody] UpdateAddressDto updateAddressDto)
        {
            try
            {
                var address = await _userService.UpdateAddressAsync(addressId, updateAddressDto);
                return Ok(address);
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
                _logger.LogError(ex, "Error al actualizar dirección {AddressId}", addressId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpDelete("addresses/{addressId:guid}")]
        [PermissionAuthorize("USER.EDIT")]
        public async Task<IActionResult> DeleteAddress(Guid addressId)
        {
            try
            {
                var deleted = await _userService.DeleteAddressAsync(addressId);
                if (!deleted)
                    return NotFound($"Dirección con ID {addressId} no encontrada");

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar dirección {AddressId}", addressId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("addresses/{addressId:guid}/set-primary")]
        [PermissionAuthorize("USER.EDIT")]
        public async Task<ActionResult<UserAddressDto>> SetPrimaryAddress(Guid addressId)
        {
            try
            {
                var address = await _userService.SetPrimaryAddressAsync(addressId);
                return Ok(address);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al establecer dirección primaria {AddressId}", addressId);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}