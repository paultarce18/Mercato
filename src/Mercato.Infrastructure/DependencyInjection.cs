using Mercato.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Infrastructure;

/// <summary>
/// Registers everything Infrastructure provides. Each composition root (Api, Worker) calls it
/// once, so the host projects never need to know how persistence is built (TR-05).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // In Development the value comes from user-secrets, elsewhere from an environment
        // variable; it is never committed (GR-31).
        string connectionString = configuration.GetConnectionString("Mercato")
                                  ?? throw new InvalidOperationException("Connection string 'Mercato' is not configured.");

        services.AddDbContext<MercatoDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }
}
