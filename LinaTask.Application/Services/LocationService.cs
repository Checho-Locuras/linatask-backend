using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services
{
    public class LocationService : ILocationService
    {
        private readonly ILocationRepository _repository;

        public LocationService(ILocationRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<LocationItemDto>> GetCountriesAsync()
            => (await _repository.GetCountriesAsync())
                .Select(c => new LocationItemDto { Id = c.Id, Name = c.Name });

        public async Task<IEnumerable<LocationItemDto>> GetDepartmentsAsync(Guid countryId)
            => (await _repository.GetDepartmentsByCountryAsync(countryId))
                .Select(d => new LocationItemDto { Id = d.Id, Name = d.Name });

        public async Task<IEnumerable<LocationItemDto>> GetCitiesAsync(Guid departmentId)
            => (await _repository.GetCitiesByDepartmentAsync(departmentId))
                .Select(c => new LocationItemDto { Id = c.Id, Name = c.Name });

        public async Task<IEnumerable<LocationItemDto>> GetInstitutionsAsync(Guid cityId)
            => (await _repository.GetInstitutionsByCityAsync(cityId))
                .Select(i => new LocationItemDto { Id = i.Id, Name = i.Name });
    }
}
