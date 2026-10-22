using CookingDirections.Application.Abstractions;
using CookingDirections.Infrastructure.Data;
using CookingDirections.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CookingDirections.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "CookingDirectionsDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");
        }

        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddScoped<ICookingDirectionRepository, CookingDirectionRepository>();
        return services;
    }
}
