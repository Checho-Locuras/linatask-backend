using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinaTask.Domain.Models
{
    public class Offer
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid TaskId { get; set; }

        [ForeignKey("TaskId")]
        public virtual TaskU Task { get; set; }  // Cambiado de Task a TaskU

        [Required]
        public Guid TeacherId { get; set; }

        [ForeignKey("TeacherId")]
        public virtual User Teacher { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Required]
        public decimal Price { get; set; }

        public string Message { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
