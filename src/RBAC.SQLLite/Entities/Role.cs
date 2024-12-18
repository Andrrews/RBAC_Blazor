using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace RBAC.SQLLite.Entities
{
    public class Role : IdentityRole
    {
        public string RoleDescription { get; set; } = string.Empty; // Opis roli
        
   
     

    }
}
