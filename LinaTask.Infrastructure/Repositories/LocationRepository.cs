using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Infrastructure.Repositories
{
    public class LocationRepository : ILocationRepository
    {
        private readonly LinaTaskDbContext _context;

        public LocationRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Country>> GetCountriesAsync()
        {
            return await _context.Countries
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Department>> GetDepartmentsByCountryAsync(Guid countryId)
        {
            return await _context.Departments
                .Where(d => d.CountryId == countryId)
                .OrderBy(d => d.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<City>> GetCitiesByDepartmentAsync(Guid departmentId)
        {
            return await _context.Cities
                .Where(c => c.DepartmentId == departmentId)
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Institution>> GetInstitutionsByCityAsync(Guid cityId)
        {
            return await _context.Institutions
                .Where(i => i.CityId == cityId)
                .OrderBy(i => i.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> ExistsInstitutionAsync(Guid id)
        {
            return await _context.Institutions
                .AnyAsync(i => i.Id == id);
        }

        public async Task<City?> GetCityByIdAsync(Guid cityId)
        {
            return await _context.Cities
                .Include(c => c.Department)
                    .ThenInclude(d => d.Country)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == cityId);
        }

        public async Task<Institution?> GetInstitutionByIdAsync(Guid institutionId)
        {
            return await _context.Institutions
                .Include(i => i.City)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == institutionId);
        }

        public async Task<Country?> GetCountryByIdAsync(Guid countryId)
        {
            return await _context.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == countryId);
        }

        public async Task<Department?> GetDepartmentByIdAsync(Guid departmentId)
        {
            return await _context.Departments
                .Include(d => d.Country)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == departmentId);
        }
    }
}
