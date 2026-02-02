using LinaTask.Domain.DTOs;

namespace LinaTask.Application.Services.Interfaces
{
    public interface ILocationService
    {
        Task<IEnumerable<LocationItemDto>> GetCitiesAsync(Guid departmentId);
        Task<IEnumerable<LocationItemDto>> GetCountriesAsync();
        Task<IEnumerable<LocationItemDto>> GetDepartmentsAsync(Guid countryId);
        Task<IEnumerable<LocationItemDto>> GetInstitutionsAsync(Guid cityId);
    }
}