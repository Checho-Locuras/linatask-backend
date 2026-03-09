using System;
using System.Collections.Generic;

namespace LinaTask.Domain.Models
{
    public class Menu
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public Guid? ParentId { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navegación
        public virtual Menu? Parent { get; set; }
        public virtual ICollection<Menu> Children { get; set; } = new List<Menu>();
        public virtual ICollection<MenuPermission> MenuPermissions { get; set; } = new List<MenuPermission>();
    }

    public class MenuPermission
    {
        public Guid MenuId { get; set; }
        public Guid PermissionId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navegación
        public virtual Menu? Menu { get; set; }
        public virtual Permission? Permission { get; set; }
    }
}