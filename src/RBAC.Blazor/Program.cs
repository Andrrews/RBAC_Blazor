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
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Identity;
using RBAC.SQLLite.Entities;
using Microsoft.AspNetCore.Components.Server;

namespace RBAC.Blazor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Dynamiczna �cie�ka do bazy danych w folderze solucji
            var solutionDirectory = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.FullName;
            var databasePath = Path.Combine(solutionDirectory, "src", "rbac.db");
    

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddDbContextFactory<DatabaseContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));

            builder.Services.AddIdentity<User, Role>()
                .AddEntityFrameworkStores<DatabaseContext>();
                 
            builder.Services.AddScoped<AuthenticationStateProvider, CustomRevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
            builder.Services.AddScoped<IHostEnvironmentAuthenticationStateProvider>(sp => {
                // this is safe because 
                //     the `RevalidatingIdentityAuthenticationStateProvider` extends the `ServerAuthenticationStateProvider`
                var provider = (ServerAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>();
                return provider;
            });
            builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationMiddlewareResultHandler>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate();
            builder.Services.AddAuthorization();

 

            //builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<DatabaseContext>();
                var userManager = services.GetRequiredService<UserManager<User>>();
                var roleManager = services.GetRequiredService<RoleManager<Role>>();

                await DatabaseSeeder.SeedAsync(context, userManager, roleManager);
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

            app.MapBlazorHub();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}