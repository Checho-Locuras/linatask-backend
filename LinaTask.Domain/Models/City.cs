using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LinaTask.Domain.Models
{
    public class City
    {
        [Key]
        public Guid Id { get; set; }

        public Guid DepartmentId { get; set; }
        public Department Department { get; set; }

        [Required]
        public string Name { get; set; }

        public ICollection<Institution> Institutions { get; set; } = new List<Institution>();
    }

}
