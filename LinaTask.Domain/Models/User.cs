using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace LinaTask.Domain.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(150)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; }
        public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; }
        = new List<TeacherSubject>();

        [Column(TypeName = "decimal(2,1)")]
        public decimal? Rating { get; set; }

        public DateTime CreatedAt { get; set; }

        // Propiedades de navegación
        public virtual TeacherProfile TeacherProfile { get; set; }
        public virtual ICollection<TaskU> TasksAsStudent { get; set; }
        public virtual ICollection<Offer> Offers { get; set; }
        public virtual ICollection<Payment> PaymentsAsStudent { get; set; }
        public virtual ICollection<TutoringSession> TutoringSessionsAsStudent { get; set; }
        public virtual ICollection<TutoringSession> TutoringSessionsAsTeacher { get; set; }
    }
}
