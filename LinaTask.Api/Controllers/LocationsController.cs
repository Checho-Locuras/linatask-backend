using LinaTask.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/locations")]
    public class LocationsController : ControllerBase
    {
        private readonly ILocationService _service;

        public LocationsController(ILocationService service)
        {
            _service = service;
        }

        [HttpGet("countries")]
        public async Task<IActionResult> GetCountries()
            => Ok(await _service.GetCountriesAsync());

        [HttpGet("departments/{countryId}")]
        public async Task<IActionResult> GetDepartments(Guid countryId)
            => Ok(await _service.GetDepartmentsAsync(countryId));

        [HttpGet("cities/{departmentId}")]
        public async Task<IActionResult> GetCities(Guid departmentId)
            => Ok(await _service.GetCitiesAsync(departmentId));

        [HttpGet("institutions/{cityId}")]
        public async Task<IActionResult> GetInstitutions(Guid cityId)
            => Ok(await _service.GetInstitutionsAsync(cityId));
    }

}
