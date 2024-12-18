using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace RBAC.SQLLite.Entities
{
    public class User : IdentityUser
    {
        public string FullName { get; set; } = string.Empty; // Dodatkowa właściwość
        public string NTLogin { get; set; } = string.Empty;  // Logowanie domenowe
        
        // Relacja z Role
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}
