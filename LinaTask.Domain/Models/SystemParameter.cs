// Domain/Models/SystemParameter.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace LinaTask.Domain.Models
{
    public class SystemParameter
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ParamKey { get; set; } = string.Empty;

        [MaxLength(500)]
        public string ParamValue { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(50)]
        public string DataType { get; set; } = "string"; // string, int, bool, decimal, json

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Helper method to get typed value
        public object GetTypedValue()
        {
            return DataType.ToLower() switch
            {
                "int" => int.TryParse(ParamValue, out int intValue) ? intValue : 0,
                "bool" => bool.TryParse(ParamValue, out bool boolValue) && boolValue,
                "decimal" => decimal.TryParse(ParamValue, out decimal decimalValue) ? decimalValue : 0m,
                "json" => ParamValue,
                _ => ParamValue
            };
        }
    }
}