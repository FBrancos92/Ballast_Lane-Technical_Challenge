using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskManager.Application.Abstractions;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Security;

namespace TaskManager.Infrastructure;

public static class DependencyInjection
{
    /// <param name="contentRootPath">A relative SQLite file path is resolved against this folder,
    /// so the database lands in the same place whatever directory the app is started from.</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "Database:ConnectionString is required.")
            .PostConfigure(o => o.ConnectionString = ResolveDataSource(o.ConnectionString, contentRootPath))
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => Encoding.UTF8.GetByteCount(o.SigningKey) >= 32, "Jwt:SigningKey must be at least 32 bytes.")
            .Validate(o => o.ExpiryMinutes > 0, "Jwt:ExpiryMinutes must be positive.")
            .ValidateOnStart();

        services.AddSingleton(sp =>
            new SqliteConnectionFactory(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString));
        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<ITaskRepository, SqliteTaskRepository>();
        services.AddScoped<IUserRepository, SqliteUserRepository>();
        services.AddScoped<DataSeeder>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }

    private static string ResolveDataSource(string connectionString, string basePath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;
        if (string.IsNullOrEmpty(dataSource)
            || dataSource == ":memory:"
            || builder.Mode == SqliteOpenMode.Memory
            || Path.IsPathRooted(dataSource))
        {
            return connectionString;
        }

        builder.DataSource = Path.GetFullPath(Path.Combine(basePath, dataSource));
        return builder.ToString();
    }
}
