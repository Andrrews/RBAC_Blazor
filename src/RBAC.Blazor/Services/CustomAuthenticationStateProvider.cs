using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RBAC.SQLLite;

namespace RBAC.Blazor.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
        public CustomAuthenticationStateProvider(IDbContextFactory<DatabaseContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;

        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var ntLogin = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            using var context = await _dbContextFactory.CreateDbContextAsync();
            var user = await context.Users
                .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions) // Załaduj uprawnienia dla każdej roli
                .FirstOrDefaultAsync(u => u.NTLogin == ntLogin.ToLower());


            if (user == null)
            {
                // Brak użytkownika – zwracamy stan anonimowy
                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                return new AuthenticationState(anonymous);
            }




            // Użytkownik znaleziony – autoryzowany
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("NTLogin", user.NTLogin)
            };

            // Dodaj role jako claims
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.RoleName));

                // Dodanie permissions na podstawie ról
                foreach (var permission in role.Permissions)
                {
                    claims.Add(new Claim("permission", permission.Name));
                }
            }
#if DEBUG
            foreach (var role in user.Roles)
            {
                Console.WriteLine($"RoleName: '{role.RoleName}' (Length: {role.RoleName.Length})");
            }

            foreach (var claim in claims)
            {
                Console.WriteLine($"Claim Type: {claim.Type}, Value: '{claim.Value}'");
            }
#endif

            var identity = new ClaimsIdentity(claims, "WindowsAuth");
            var authenticatedUser = new ClaimsPrincipal(identity);


            Console.WriteLine($"IsAuthenticated: {authenticatedUser.Identity?.IsAuthenticated}, Name: {authenticatedUser.Identity?.Name}, Roles: {string.Join(", ", authenticatedUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value))}");
            return new AuthenticationState(authenticatedUser);

        }

    }


}
