using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LinaTask.Domain.DTOs
{
    public class UpdateUserDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public decimal? Rating { get; set; }
        public bool? IsActive { get; set; }

        [MinLength(6)]
        public string? Password { get; set; }
        public string? ProfilePhoto { get; set; }
    }
}
