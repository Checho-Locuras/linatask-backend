// Application/Services/SystemParameterService.cs
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinaTask.Application.Services
{
    public class SystemParameterService : ISystemParameterService
    {
        private readonly ISystemParameterRepository _parameterRepository;
        private readonly ILogger<SystemParameterService> _logger;

        public SystemParameterService(
            ISystemParameterRepository parameterRepository,
            ILogger<SystemParameterService> logger)
        {
            _parameterRepository = parameterRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<SystemParameterDto>> GetAllParametersAsync()
        {
            try
            {
                var parameters = await _parameterRepository.GetAllAsync();
                return parameters.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los parámetros");
                throw;
            }
        }

        public async Task<IEnumerable<SystemParameterDto>> GetActiveParametersAsync()
        {
            try
            {
                var parameters = await _parameterRepository.GetActiveAsync();
                return parameters.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener parámetros activos");
                throw;
            }
        }

        public async Task<SystemParameterDto?> GetParameterByIdAsync(Guid id)
        {
            try
            {
                var parameter = await _parameterRepository.GetByIdAsync(id);
                return parameter == null ? null : MapToDto(parameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener parámetro por ID: {ParameterId}", id);
                throw;
            }
        }

        public async Task<SystemParameterDto?> GetParameterByKeyAsync(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentException("La clave no puede estar vacía");

                var parameter = await _parameterRepository.GetByKeyAsync(key);
                return parameter == null ? null : MapToDto(parameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener parámetro por clave: {Key}", key);
                throw;
            }
        }

        public async Task<object?> GetParameterValueAsync(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentException("La clave no puede estar vacía");

                var parameter = await _parameterRepository.GetByKeyAsync(key);
                if (parameter == null || !parameter.IsActive)
                    return null;

                return parameter.GetTypedValue();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener valor del parámetro: {Key}", key);
                throw;
            }
        }

        public async Task<SystemParameterDto> CreateParameterAsync(CreateSystemParameterDto createDto)
        {
            try
            {
                // Validar que la clave no exista
                var existingParameter = await _parameterRepository.GetByKeyAsync(createDto.ParamKey);
                if (existingParameter != null)
                    throw new InvalidOperationException($"Ya existe un parámetro con la clave '{createDto.ParamKey}'");

                // Validar tipo de dato
                var validDataTypes = new[] { "string", "int", "bool", "decimal", "json" };
                if (!validDataTypes.Contains(createDto.DataType.ToLower()))
                    throw new InvalidOperationException($"Tipo de dato inválido. Valores permitidos: {string.Join(", ", validDataTypes)}");

                // Validar valor según el tipo de dato
                ValidateParameterValue(createDto.ParamValue, createDto.DataType);

                // Crear nuevo parámetro
                var parameter = new SystemParameter
                {
                    Id = Guid.NewGuid(),
                    ParamKey = createDto.ParamKey.Trim().ToLower(),
                    ParamValue = createDto.ParamValue.Trim(),
                    Description = createDto.Description?.Trim() ?? string.Empty,
                    DataType = createDto.DataType.ToLower(),
                    IsActive = createDto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                var createdParameter = await _parameterRepository.CreateAsync(parameter);
                return MapToDto(createdParameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear parámetro: {Key}", createDto.ParamKey);
                throw;
            }
        }

        public async Task<SystemParameterDto> UpdateParameterAsync(Guid id, UpdateSystemParameterDto updateDto)
        {
            try
            {
                var parameter = await _parameterRepository.GetByIdAsync(id);
                if (parameter == null)
                    throw new KeyNotFoundException($"Parámetro con ID {id} no encontrado");

                // Validar tipo de dato si se cambia
                if (!string.IsNullOrWhiteSpace(updateDto.DataType))
                {
                    var validDataTypes = new[] { "string", "int", "bool", "decimal", "json" };
                    if (!validDataTypes.Contains(updateDto.DataType.ToLower()))
                        throw new InvalidOperationException($"Tipo de dato inválido. Valores permitidos: {string.Join(", ", validDataTypes)}");

                    parameter.DataType = updateDto.DataType.ToLower();
                }

                // Validar valor si se cambia
                if (!string.IsNullOrWhiteSpace(updateDto.ParamValue))
                {
                    ValidateParameterValue(updateDto.ParamValue, parameter.DataType);
                    parameter.ParamValue = updateDto.ParamValue.Trim();
                }

                // Actualizar otros campos
                if (!string.IsNullOrWhiteSpace(updateDto.Description))
                    parameter.Description = updateDto.Description.Trim();

                if (updateDto.IsActive.HasValue)
                    parameter.IsActive = updateDto.IsActive.Value;

                parameter.UpdatedAt = DateTime.UtcNow;

                var updatedParameter = await _parameterRepository.UpdateAsync(parameter);
                return MapToDto(updatedParameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar parámetro: {ParameterId}", id);
                throw;
            }
        }

        public async Task<SystemParameterDto> UpdateParameterByKeyAsync(string key, UpdateSystemParameterDto updateDto)
        {
            try
            {
                var parameter = await _parameterRepository.GetByKeyAsync(key);
                if (parameter == null)
                    throw new KeyNotFoundException($"Parámetro con clave '{key}' no encontrado");

                return await UpdateParameterAsync(parameter.Id, updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar parámetro por clave: {Key}", key);
                throw;
            }
        }

        public async Task<bool> DeleteParameterAsync(Guid id)
        {
            try
            {
                return await _parameterRepository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar parámetro: {ParameterId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteParameterByKeyAsync(string key)
        {
            try
            {
                var parameter = await _parameterRepository.GetByKeyAsync(key);
                if (parameter == null)
                    return false;

                return await _parameterRepository.DeleteAsync(parameter.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar parámetro por clave: {Key}", key);
                throw;
            }
        }

        public async Task<IEnumerable<SystemParameterDto>> SearchParametersAsync(SystemParameterSearchDto searchDto)
        {
            try
            {
                IEnumerable<SystemParameter> parameters;

                if (string.IsNullOrWhiteSpace(searchDto.SearchTerm))
                {
                    parameters = await _parameterRepository.GetAllAsync();
                }
                else
                {
                    parameters = await _parameterRepository.SearchAsync(searchDto.SearchTerm);
                }

                // Filtrar por estado activo si se especifica
                if (searchDto.IsActive.HasValue)
                {
                    parameters = parameters.Where(p => p.IsActive == searchDto.IsActive.Value);
                }

                // Filtrar por tipo de dato si se especifica
                if (!string.IsNullOrWhiteSpace(searchDto.DataType))
                {
                    parameters = parameters.Where(p => p.DataType.Equals(searchDto.DataType, StringComparison.OrdinalIgnoreCase));
                }

                return parameters.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar parámetros");
                throw;
            }
        }

        private void ValidateParameterValue(string value, string dataType)
        {
            switch (dataType.ToLower())
            {
                case "int":
                    if (!int.TryParse(value, out _))
                        throw new InvalidOperationException("El valor debe ser un número entero válido");
                    break;

                case "bool":
                    if (!bool.TryParse(value, out _))
                        throw new InvalidOperationException("El valor debe ser 'true' o 'false'");
                    break;

                case "decimal":
                    if (!decimal.TryParse(value, out _))
                        throw new InvalidOperationException("El valor debe ser un número decimal válido");
                    break;

                case "json":
                    // Validación básica de JSON (podrías usar Newtonsoft.Json o System.Text.Json)
                    if (!value.Trim().StartsWith("{") && !value.Trim().StartsWith("["))
                        throw new InvalidOperationException("El valor debe ser un JSON válido");
                    break;

                case "string":
                    // No se necesita validación especial
                    break;

                default:
                    throw new InvalidOperationException($"Tipo de dato no soportado: {dataType}");
            }
        }

        private static SystemParameterDto MapToDto(SystemParameter parameter)
        {
            return new SystemParameterDto
            {
                Id = parameter.Id,
                ParamKey = parameter.ParamKey,
                ParamValue = parameter.ParamValue,
                Description = parameter.Description,
                DataType = parameter.DataType,
                IsActive = parameter.IsActive,
                CreatedAt = parameter.CreatedAt,
                UpdatedAt = parameter.UpdatedAt
            };
        }
    }
}