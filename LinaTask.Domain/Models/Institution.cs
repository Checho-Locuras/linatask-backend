using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LinaTask.Domain.Models
{
    public class Institution
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CityId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(50)]
        public string? Type { get; set; }

        public DateTime CreatedAt { get; set; }

        // Relaciones
        public City City { get; set; } = null!;
    }

}
