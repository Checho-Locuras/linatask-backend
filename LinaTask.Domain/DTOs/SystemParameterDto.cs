// Domain/DTOs/SystemParameterDtos.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace LinaTask.Domain.DTOs
{
    public class SystemParameterDto
    {
        public Guid Id { get; set; }
        public string ParamKey { get; set; } = string.Empty;
        public string ParamValue { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateSystemParameterDto
    {
        [Required(ErrorMessage = "La clave es requerida")]
        [MaxLength(100, ErrorMessage = "La clave no puede exceder 100 caracteres")]
        [RegularExpression(@"^[a-zA-Z0-9_\-\.]+$", ErrorMessage = "Solo letras, números, guiones, puntos y guiones bajos")]
        public string ParamKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "El valor es requerido")]
        [MaxLength(500, ErrorMessage = "El valor no puede exceder 500 caracteres")]
        public string ParamValue { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de dato es requerido")]
        [MaxLength(50, ErrorMessage = "El tipo de dato no puede exceder 50 caracteres")]
        public string DataType { get; set; } = "string";

        public bool IsActive { get; set; } = true;
    }

    public class UpdateSystemParameterDto
    {
        [MaxLength(500, ErrorMessage = "El valor no puede exceder 500 caracteres")]
        public string? ParamValue { get; set; }

        [MaxLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Description { get; set; }

        [MaxLength(50, ErrorMessage = "El tipo de dato no puede exceder 50 caracteres")]
        public string? DataType { get; set; }

        public bool? IsActive { get; set; }
    }

    public class SystemParameterSearchDto
    {
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public string? DataType { get; set; }
    }
}