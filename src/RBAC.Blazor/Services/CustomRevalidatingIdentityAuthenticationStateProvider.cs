using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RBAC.SQLLite;
using RBAC.SQLLite.Entities;

namespace RBAC.Blazor.Services
{
    public class CustomRevalidatingIdentityAuthenticationStateProvider<TUser>
    : RevalidatingServerAuthenticationStateProvider where TUser : class
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IdentityOptions _options;

        public CustomRevalidatingIdentityAuthenticationStateProvider(
            ILoggerFactory loggerFactory,
            IServiceScopeFactory scopeFactory,
            IOptions<IdentityOptions> optionsAccessor)
            : base(loggerFactory)
        {
            _scopeFactory = scopeFactory;
            _options = optionsAccessor.Value;
        }

        protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

        protected override async Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            var scope = _scopeFactory.CreateScope();
            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

                // Pobranie NTLogin
                var ntLogin = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                if (string.IsNullOrEmpty(ntLogin))
                {
                    return false; // Brak użytkownika
                }

                // Pobranie użytkownika na podstawie NTLogin
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.NTLogin.ToLower() == ntLogin.ToLower(), cancellationToken);
                var usermanager = await userManager.FindByNameAsync(user.FullName);
                if (user == null)
                {
                    return false; // Użytkownik nie istnieje
                }

                // Weryfikacja security stamp
                if (userManager.SupportsUserSecurityStamp)
                {
                    var principalStamp = authenticationState.User.FindFirstValue(_options.ClaimsIdentity.SecurityStampClaimType);
                    var userStamp = await userManager.GetSecurityStampAsync(usermanager);

                    return principalStamp == userStamp;
                }

                return true; // Użytkownik jest ważny
            }
            finally
            {
                if (scope is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else
                {
                    scope.Dispose();
                }
            }
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            // Pobranie NTLogin
            var ntLogin = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            if (string.IsNullOrEmpty(ntLogin))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Anonimowy użytkownik
            }

            // Pobranie użytkownika z bazy
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.NTLogin.ToLower() == ntLogin.ToLower());
            var usermanager = await userManager.FindByNameAsync(user.UserName);
            if (user == null)
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Anonimowy użytkownik
            }

            // Tworzenie listy claimów
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("FullName", user.FullName),
                new Claim("NTLogin", user.NTLogin)
            };

            // Pobranie ról i przypisanych do nich claimów
            var roles = await userManager.GetRolesAsync(usermanager);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "WindowsAuth");
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }
    }

}
