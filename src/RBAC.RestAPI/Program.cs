using Microsoft.EntityFrameworkCore;
using RBAC.SQLLite;
using RBAC.SQLLite.Entities;
using RBAC.SQLLite.Seeds;


namespace RBAC.RestAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var solutionDirectory = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.FullName;
            var databasePath = Path.Combine(solutionDirectory,"RBAC", "rbac.db");
            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<DatabaseContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));
            var app = builder.Build();


            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                DatabaseSeeder.Seed(context);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
