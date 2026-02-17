using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Models
{
    public class RolePermission
    {
        public Guid RoleId { get; set; }
        public Role Role { get; set; }

        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; }
    }

}
