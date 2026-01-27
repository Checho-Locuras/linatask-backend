using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinaTask.Domain.Models
{
    public class TaskU
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual User Student { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; }

        public string Description { get; set; }

        [MaxLength(100)]
        public string Subject { get; set; }

        public DateTime? Deadline { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Budget { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Propiedades de navegación
        public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
