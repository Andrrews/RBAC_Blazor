using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RBAC.SQLLite.Entities
{
    public class Role
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }

        public ICollection<Permission> Permissions { get; set; }
        public ICollection<User> Users { get; set; }

    }
}
