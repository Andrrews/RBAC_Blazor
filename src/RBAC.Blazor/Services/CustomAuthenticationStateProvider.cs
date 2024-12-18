using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RBAC.SQLLite.Entities;

namespace RBAC.Blazor.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomAuthenticationStateProvider(UserManager<User> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            // Pobranie bieżącego użytkownika Windows (domenowego)
            var ntLogin = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            if (string.IsNullOrEmpty(ntLogin))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Znajdź użytkownika w Identity bazując na NTLogin
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.NTLogin.ToLower() == ntLogin.ToLower());

            if (user == null)
            {
                // Brak użytkownika – zwracamy anonimowego
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Tworzenie claimów
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName ?? user.UserName),
                new Claim("NTLogin", user.NTLogin)
            };

            // Pobieranie ról użytkownika
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));

                // Pobranie claimów przypisanych do roli (AspNetRoleClaims)
                var roleClaims = await _userManager.GetClaimsAsync(user);
                foreach (var claim in roleClaims)
                {
                    claims.Add(claim);
                }
            }

            // Tworzenie ClaimsPrincipal
            var identity = new ClaimsIdentity(claims, "WindowsAuth");
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }
    }
}
