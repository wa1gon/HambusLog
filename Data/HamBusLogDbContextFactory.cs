using System;
using Microsoft.EntityFrameworkCore;

namespace HamBusLog.Data;

/// <summary>
/// Supported database providers.
/// </summary>
public enum DatabaseProvider
{
    Sqlite,
    PostgreSQL
}

/// <summary>
/// Creates <see cref="HamBusLogDbContext"/> instances configured for either SQLite or PostgreSQL.
/// </summary>
public static class HamBusLogDbContextFactory
{
    /// <summary>
    /// Creates a new <see cref="HamBusLogDbContext"/> using the specified provider and connection string.
    /// </summary>
    /// <param name="provider">Target database provider.</param>
    /// <param name="connectionString">Connection string for the target database.</param>
    /// <returns>A configured <see cref="HamBusLogDbContext"/>.</returns>
    public static HamBusLogDbContext Create(DatabaseProvider provider, string connectionString)
    {
        var options = BuildOptions(provider, connectionString);
        return new HamBusLogDbContext(options);
    }

    /// <summary>
    /// Builds <see cref="DbContextOptions{TContext}"/> for the specified provider.
    /// </summary>
    public static DbContextOptions<HamBusLogDbContext> BuildOptions(
        DatabaseProvider provider,
        string connectionString)
    {
        var builder = new DbContextOptionsBuilder<HamBusLogDbContext>();

        switch (provider)
        {
            case DatabaseProvider.Sqlite:
                builder.UseSqlite(connectionString);
                break;

            case DatabaseProvider.PostgreSQL:
                builder.UseNpgsql(connectionString);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported database provider.");
        }

        return builder.Options;
    }
}


