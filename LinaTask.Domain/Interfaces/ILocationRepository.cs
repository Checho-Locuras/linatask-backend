using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Interfaces
{
    public interface ILocationRepository
    {
        Task<IEnumerable<Country>> GetCountriesAsync();
        Task<IEnumerable<Department>> GetDepartmentsByCountryAsync(Guid countryId);
        Task<IEnumerable<City>> GetCitiesByDepartmentAsync(Guid departmentId);
        Task<IEnumerable<Institution>> GetInstitutionsByCityAsync(Guid cityId);

        Task<City?> GetCityByIdAsync(Guid cityId);
        Task<Institution?> GetInstitutionByIdAsync(Guid institutionId);
        Task<Country?> GetCountryByIdAsync(Guid countryId);
        Task<Department?> GetDepartmentByIdAsync(Guid departmentId);
        Task<bool> ExistsInstitutionAsync(Guid id);

    }
}
