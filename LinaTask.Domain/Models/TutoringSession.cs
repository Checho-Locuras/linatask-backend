using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace LinaTask.Domain.Models
{
    public class TutoringSession
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual User Student { get; set; }

        [Required]
        public Guid TeacherId { get; set; }

        [ForeignKey("TeacherId")]
        public virtual User Teacher { get; set; }

        public virtual DateTime CreatedAt { get; set; }

        public DateTime SessionDate { get; set; }

        public string MeetLink { get; set; }

        [MaxLength(30)]
        public string Status { get; set; }


    }
}
