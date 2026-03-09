using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Models
{
    public class Permission
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public string? Description { get; set; }
        public string? Module { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; }
        public virtual ICollection<MenuPermission> MenuPermissions { get; set; } = new List<MenuPermission>();
    }

}
