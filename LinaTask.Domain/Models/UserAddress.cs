using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LinaTask.Domain.Models
{
    public class UserAddress
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid CityId { get; set; }

        [Required, MaxLength(255)]
        public string Address { get; set; } = null!;

        public bool IsPrimary { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        // Relaciones
        public User User { get; set; } = null!;
        public City City { get; set; } = null!;
    }

}
