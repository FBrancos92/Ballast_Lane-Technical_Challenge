using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure;

public static class HostExtensions
{
    /// <summary>Creates the schema and, unless <c>Seed:Enabled</c> is false, seeds demo data.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(cancellationToken);

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (configuration.GetValue("Seed:Enabled", defaultValue: true))
        {
            await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync(cancellationToken);
        }
    }
}
