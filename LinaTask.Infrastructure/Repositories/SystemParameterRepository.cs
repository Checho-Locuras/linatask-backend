// Infrastructure/Repositories/SystemParameterRepository.cs
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinaTask.Infrastructure.Repositories
{
    public class SystemParameterRepository : ISystemParameterRepository
    {
        private readonly LinaTaskDbContext _context;

        public SystemParameterRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SystemParameter>> GetAllAsync()
        {
            try
            {
                return await _context.SystemParameters
                    .AsNoTracking()
                    .OrderBy(p => p.ParamKey)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al obtener todos los parámetros: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<SystemParameter>> GetActiveAsync()
        {
            try
            {
                return await _context.SystemParameters
                    .Where(p => p.IsActive)
                    .AsNoTracking()
                    .OrderBy(p => p.ParamKey)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al obtener parámetros activos: {ex.Message}", ex);
            }
        }

        public async Task<SystemParameter?> GetByIdAsync(Guid id)
        {
            return await _context.SystemParameters.FindAsync(id);
        }

        public async Task<SystemParameter?> GetByKeyAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("La clave no puede estar vacía", nameof(key));

            return await _context.SystemParameters
                .FirstOrDefaultAsync(p => p.ParamKey.ToLower() == key.ToLower());
        }

        public async Task<SystemParameter> CreateAsync(SystemParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            try
            {
                parameter.CreatedAt = DateTime.UtcNow;
                parameter.UpdatedAt = null;

                _context.SystemParameters.Add(parameter);
                await _context.SaveChangesAsync();

                return parameter;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al crear el parámetro: {ex.Message}", ex);
            }
        }

        public async Task<SystemParameter> UpdateAsync(SystemParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            try
            {
                parameter.UpdatedAt = DateTime.UtcNow;

                _context.SystemParameters.Update(parameter);
                await _context.SaveChangesAsync();

                return parameter;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al actualizar el parámetro: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var parameter = await _context.SystemParameters.FindAsync(id);
            if (parameter == null)
                return false;

            try
            {
                _context.SystemParameters.Remove(parameter);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al eliminar el parámetro: {ex.Message}", ex);
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.SystemParameters.AnyAsync(p => p.Id == id);
        }

        public async Task<bool> ExistsByKeyAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("La clave no puede estar vacía", nameof(key));

            return await _context.SystemParameters
                .AnyAsync(p => p.ParamKey.ToLower() == key.ToLower());
        }

        public async Task<IEnumerable<SystemParameter>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            return await _context.SystemParameters
                .Where(p => p.ParamKey.Contains(searchTerm) ||
                           p.Description.Contains(searchTerm) ||
                           p.ParamValue.Contains(searchTerm))
                .AsNoTracking()
                .OrderBy(p => p.ParamKey)
                .ToListAsync();
        }

        public async Task<SystemParameter?> GetByKeyAndTypeAsync(string key, string dataType)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("La clave no puede estar vacía", nameof(key));

            return await _context.SystemParameters
                .FirstOrDefaultAsync(p => p.ParamKey.ToLower() == key.ToLower() &&
                                        p.DataType.ToLower() == dataType.ToLower());
        }
    }
}