using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.DTOs
{
    public class UserAddressDto
    {
        public Guid Id { get; set; }
        public Guid CityId { get; set; }
        public string Address { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
