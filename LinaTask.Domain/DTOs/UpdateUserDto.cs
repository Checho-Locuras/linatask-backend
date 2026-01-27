using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.DTOs
{
    public class UpdateUserDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public decimal? Rating { get; set; }
    }
}
