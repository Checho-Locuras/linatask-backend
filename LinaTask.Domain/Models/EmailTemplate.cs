// LinaTask.Domain/Models/EmailTemplate.cs
using System.ComponentModel.DataAnnotations;

namespace LinaTask.Domain.Models
{
    public class EmailTemplate
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>Clave única para identificar el template. Ej: "session_reminder"</summary>
        [Required, MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>Asunto del correo. Soporta variables: {{UserName}}, {{SubjectName}}, etc.</summary>
        [Required, MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>Cuerpo HTML. Soporta las mismas variables.</summary>
        [Required]
        public string HtmlBody { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}