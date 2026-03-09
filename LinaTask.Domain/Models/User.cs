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

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required, MaxLength(150)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public DateTime BirthDate { get; set; }   // 👈 NUEVO

        [Column(TypeName = "text")]
        public string? ProfilePhoto { get; set; }

        [Column(TypeName = "decimal(2,1)")]
        public decimal? Rating { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        // Relaciones
        public ICollection<UserAcademicProfile> AcademicProfiles { get; set; }
            = new List<UserAcademicProfile>();



        public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; }
        = new List<TeacherSubject>();

        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; }
        = new List<PasswordResetToken>();

        public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();


        // Propiedades de navegación
        public virtual TeacherProfile TeacherProfile { get; set; }
        public virtual ICollection<TaskU> TasksAsStudent { get; set; }
        public virtual ICollection<Offer> Offers { get; set; }
        public virtual ICollection<Payment> PaymentsAsStudent { get; set; }
        public virtual ICollection<TutoringSession> TutoringSessionsAsStudent { get; set; }
        public virtual ICollection<TutoringSession> TutoringSessionsAsTeacher { get; set; }
    }
}
