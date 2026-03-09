using System;
using System.Collections.Generic;

namespace LinaTask.Domain.DTOs
{
    public class MenuDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public Guid? ParentId { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; }
    }

    public class MenuWithChildrenDto : MenuDto
    {
        public List<MenuWithChildrenDto> Children { get; set; } = new();
        public List<Guid> PermissionIds { get; set; } = new();
    }

    public class CreateMenuDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public Guid? ParentId { get; set; }
        public int Order { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public List<Guid> PermissionIds { get; set; } = new();
    }

    public class UpdateMenuDto
    {
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public Guid? ParentId { get; set; }
        public int? Order { get; set; }
        public bool? IsVisible { get; set; }
        public List<Guid>? PermissionIds { get; set; }
    }
}