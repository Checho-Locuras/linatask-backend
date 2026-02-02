using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LinaTask.Domain.Models
{
    public class Department
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CountryId { get; set; }
        public Country Country { get; set; }

        [Required]
        public string Name { get; set; }

        public ICollection<City> Cities { get; set; } = new List<City>();
    }

}
