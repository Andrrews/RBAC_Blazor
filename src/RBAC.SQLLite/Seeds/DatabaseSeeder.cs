using Microsoft.EntityFrameworkCore;
using RBAC.SQLLite.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RBAC.SQLLite.Seeds
{
    public static class DatabaseSeeder
    {

        public static void Seed(DatabaseContext context)
        {
            context.Database.Migrate(); // Automatyczne stosowanie migracji

            // Dodajemy uprawnienia zawsze
            SeedPermissions(context);

            // Te metody są tylko do celów testowych
            SeedRolesForTesting(context);
            SeedUsersForTesting(context);
        }

        private static void SeedPermissions(DatabaseContext context)
        {
            // Lista uprawnień zdefiniowanych przez programistów
            var permissions = new[]
            {
                new Permission { Name = "ViewWeather", Description = "Allows viewing Weather page" },
                new Permission { Name = "ViewCounter", Description = "Allows viewing Counter page" },
                new Permission { Name = "EditCounter", Description = "Allows editing Counter page" }


            };

            // Dodanie brakujących uprawnień do bazy
            foreach (var permission in permissions)
            {
                if (!context.Permissions.Any(p => p.Name == permission.Name))
                {
                    context.Permissions.Add(permission);
                }
            }

            context.SaveChanges();
        }

        private static void SeedRolesForTesting(DatabaseContext context)
        {
            if (!context.Roles.Any())
            {
                var adminRole = new Role
                {
                    RoleName = "Admin",
                    RoleDescription = "Administrator role",
                    Permissions = context.Permissions.ToList() // Przypisz wszystkie dostępne uprawnienia
                };

                var userRole = new Role
                {
                    RoleName = "User",
                    RoleDescription = "Regular user role",
                    Permissions = context.Permissions
                        .Where(p => p.Name.StartsWith("View")) // Tylko uprawnienia do odczytu
                        .ToList()
                };

                context.Roles.AddRange(adminRole, userRole);
                context.SaveChanges();
            }
        }

        private static void SeedUsersForTesting(DatabaseContext context)
        {
            if (!context.Users.Any(u => u.Username == "admin"))
            {
                var adminRole = context.Roles.FirstOrDefault(r => r.RoleName == "Admin");
                var userRole = context.Roles.FirstOrDefault(r => r.RoleName == "User");

                if (adminRole == null || userRole == null) return;

                // Użytkownik Admin
                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = "hashed_password",
                    NTLogin = @""
                };
                adminUser.Roles.Add(adminRole);

                // Użytkownik User
                var regularUser = new User
                {
                    Username = "user",
                    Email = "user@example.com",
                    PasswordHash = "hashed_password"
                };
                regularUser.Roles.Add(userRole);

                context.Users.AddRange(adminUser, regularUser);
                context.SaveChanges();
            }
        }
    }
}
