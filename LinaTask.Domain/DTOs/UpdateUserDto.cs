using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LinaTask.Domain.DTOs
{
    public class UpdateUserDto
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public decimal? Rating { get; set; }
        public bool? IsActive { get; set; }

        [MinLength(6)]
        public string? Password { get; set; }

        public string? ProfilePhoto { get; set; }
        public DateTime? BirthDate { get; set; }

        // Nueva propiedad para roles
        public IEnumerable<Guid>? RoleIds { get; set; }
    }
}
