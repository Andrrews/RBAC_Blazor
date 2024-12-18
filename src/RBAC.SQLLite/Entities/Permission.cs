using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RBAC.SQLLite.Entities
{
    public class Permission 
    {
        public Guid Id { get; set; } = Guid.NewGuid(); 
        public string Name { get; set; }
        public string Description { get; set; }

        public ICollection<Role> Roles { get; set; } = new List<Role>();

    }
}
