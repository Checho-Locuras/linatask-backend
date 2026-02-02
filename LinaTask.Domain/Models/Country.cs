using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LinaTask.Domain.Models
{
    public class Country
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Code { get; set; }

        public ICollection<Department> Departments { get; set; } = new List<Department>();
    }

}
