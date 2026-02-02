using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LinaTask.Domain.DTOs
{
    public class CreateAddressDto
    {
        [Required]
        public Guid CityId { get; set; }

        [Required, MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;
    }

    public class UpdateAddressDto
    {
        public Guid? CityId { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        public bool? IsPrimary { get; set; }
    }
}
