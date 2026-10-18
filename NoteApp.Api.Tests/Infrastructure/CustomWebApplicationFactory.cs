using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NoteApp.Api.Data;

namespace NoteApp.Api.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"NoteAppTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Keep Jwt:* aligned with appsettings.json so token signing and
            // JwtBearer validation always use the same captured key.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\mssqllocaldb;Database=Unused;"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            RemoveDbContextRegistrations(services);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    private static void RemoveDbContextRegistrations(IServiceCollection services)
    {
        var descriptors = services
            .Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(AppDbContext) ||
                d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>))
            .ToList();

        foreach (var descriptor in descriptors)
            services.Remove(descriptor);
    }
}
