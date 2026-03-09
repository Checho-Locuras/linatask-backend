using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.DTOs
{
    public class UserDto
    {
        // =====================
        // USER
        // =====================
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal? Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string? ProfilePhoto { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? BirthDate { get; set; }

        public ICollection<UserRoleDto> UserRoles { get; set; } = new List<UserRoleDto>();

        // =====================
        // ACADEMIC PROFILES
        // =====================
        public List<UserAcademicProfileDto> AcademicProfiles { get; set; } = new();

        // =====================
        // ADDRESSES
        // =====================
        public List<UserAddressDto> Addresses { get; set; } = new();
    }

}
