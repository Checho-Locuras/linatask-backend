using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinaTask.Domain.Models
{
    public class Payment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid TaskId { get; set; }

        [ForeignKey("TaskId")]
        public virtual TaskU TaskU { get; set; }  // Cambiado de Task a TaskU

        [Required]
        public Guid StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual User Student { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Required]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Required]
        public decimal PlatformFee { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Required]
        public decimal TeacherAmount { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public Guid TeacherId { get; set; }

        [ForeignKey("TeacherId")]
        public virtual User Teacher { get; set; }
    }
}
