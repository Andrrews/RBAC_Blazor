using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using RBAC.Blazor.Components;
using RBAC.Blazor.Services;
using RBAC.SQLLite;
using RBAC.SQLLite.Seeds;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
 
namespace RBAC.Blazor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Dynamiczna œcie¿ka do bazy danych w folderze solucji
            var solutionDirectory = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.FullName;
            var databasePath = Path.Combine(solutionDirectory, "RBAC", "rbac.db");
    

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddDbContextFactory<DatabaseContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));
            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7002/") // Adres Twojego backendu
            });
            builder.Services.AddHttpClient("ServerAPI", client =>
            {
                client.BaseAddress = new Uri("https://localhost:7002/");
            });
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/accessdenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    options.SlidingExpiration = true;
                });
            builder.Services.AddAuthorization();
       

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                DatabaseSeeder.Seed(context);
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
  
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();
            app.MapPost("/login", async (HttpContext httpContext, string username) =>
            {
                if (string.IsNullOrEmpty(username))
                {
                    return Results.BadRequest("Brak nazwy u¿ytkownika.");
                }

                if (username != "admin")
                {
                    return Results.Unauthorized();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return Results.Ok("Zalogowano");
            });

            app.MapPost("/logout", async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Ok("Wylogowano");
            });

            app.MapBlazorHub();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
