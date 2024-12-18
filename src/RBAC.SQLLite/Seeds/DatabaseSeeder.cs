using Microsoft.EntityFrameworkCore;
using RBAC.SQLLite.Entities;
using Microsoft.AspNetCore.Identity;

namespace RBAC.SQLLite.Seeds
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(DatabaseContext context, UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            // Automatyczne stosowanie migracji
            await context.Database.MigrateAsync();

            // Dodanie ról i przypisanie uprawnień (claimów)
            await SeedRolesWithClaimsAsync(roleManager);

            // Dodanie użytkowników i przypisanie ról
            await SeedUsersAsync(userManager, roleManager);
        }

        private static async Task SeedRolesWithClaimsAsync(RoleManager<Role> roleManager)
        {
            // Role i claimy (permissions)
            var rolesWithClaims = new Dictionary<string, List<string>>
            {
                { "Admin", new List<string> { "ViewWeather", "ViewCounter", "EditCounter" } },
                { "User", new List<string> { "ViewWeather", "ViewCounter" } }
            };

            foreach (var roleEntry in rolesWithClaims)
            {
                var roleName = roleEntry.Key;
                var permissions = roleEntry.Value;

                // Dodaj rolę, jeśli nie istnieje
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new Role
                    {
                        Name = roleName,
                        RoleDescription = $"{roleName} role"
                    };

                    await roleManager.CreateAsync(role);
                }

                // Pobierz rolę
                var existingRole = await roleManager.FindByNameAsync(roleName);

                // Dodaj claimy do roli
                foreach (var permission in permissions)
                {
                    var existingClaims = await roleManager.GetClaimsAsync(existingRole);
                    if (!existingClaims.Any(c => c.Type == "permission" && c.Value == permission))
                    {
                        await roleManager.AddClaimAsync(existingRole, new System.Security.Claims.Claim("permission", permission));
                    }
                }
            }
        }

        private static async Task SeedUsersAsync(UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            // Lista użytkowników do stworzenia
            var users = new List<(string UserName, string Email, string FullName, string NTLogin, string Role)>
            {
                ("admin", "admin@example.com", "Administrator", @"domain\user", "ADMIN"),
                ("user", "user@example.com", "Regular User", @"domain\user", "USER")
            };

            foreach (var userEntry in users)
            {
                var (userName, email, fullName, ntLogin, role) = userEntry;

                if (await userManager.FindByNameAsync(userName) == null)
                {
                    var user = new User
                    {
                        UserName = userName,
                        Email = email,
                        FullName = fullName,
                        NTLogin = ntLogin
                    };

                    // Utwórz użytkownika z domyślnym hasłem
                    var res = await userManager.CreateAsync(user, $"{userName}ABC123!");
                    if (res.Succeeded)
                    {
                        var roleExists = await roleManager.RoleExistsAsync(role);
                        if (!roleExists)
                        {
                            throw new Exception($"Role '{role}' does not exist.");
                        }

                        var userExists = await userManager.FindByNameAsync(user.UserName);
                        if (userExists == null)
                        {
                            throw new Exception($"User '{user.UserName}' does not exist.");
                        }

                        await userManager.AddToRoleAsync(user, role);
                    }
                   
                }
            }
        }
    }
}
