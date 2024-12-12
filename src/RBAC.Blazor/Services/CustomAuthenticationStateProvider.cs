using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using RBAC.SQLLite;

namespace RBAC.Blazor.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
        public CustomAuthenticationStateProvider(IDbContextFactory<DatabaseContext> dbContextFactory, HttpClient httpClient)
        {
            _dbContextFactory = dbContextFactory;
            _httpClient = httpClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var ntLogin = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            using var context = await _dbContextFactory.CreateDbContextAsync();
            var user = await context.Users.Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.NTLogin == ntLogin.ToLower());

            if (user == null)
            {
                // Użytkownik nie znaleziony – anonimowy
                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                return await Task.FromResult(new AuthenticationState(anonymous));
            }

            // Użytkownik znaleziony – autoryzowany
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("NTLogin", user.NTLogin)
            };

            // Dodaj role jako claims
            claims.AddRange(user.Roles.Select(role =>
                new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", role.RoleName)));


            var identity = new ClaimsIdentity(claims, "WindowsAuth");
            var authenticatedUser = new ClaimsPrincipal(identity);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            var response = await _httpClient.PostAsJsonAsync("/login", new { username = user.Username });

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Logowanie na backendzie nie powiodło się.");
            }


            // Backend loguje użytkownika, generując cookies

            Console.WriteLine($"IsAuthenticated: {authenticatedUser.Identity?.IsAuthenticated}, Name: {authenticatedUser.Identity?.Name}, Roles: {string.Join(", ", authenticatedUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value))}");
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(authenticatedUser)));
            return new AuthenticationState(authenticatedUser);
            //return await Task.FromResult(new AuthenticationState(authenticatedUser));
        }
        private async Task LogInUserOnBackend(ClaimsPrincipal user)
        {
           
        }
    }


}
