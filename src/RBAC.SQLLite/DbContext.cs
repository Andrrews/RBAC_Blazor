using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RBAC.SQLLite.Entities;

namespace RBAC.SQLLite
{
    public class DatabaseContext : IdentityDbContext<User, Role, string>
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

         
    }
}
 