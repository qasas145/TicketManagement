using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TicketManagement.Infrastructure.DistributedLock;
using TicketManagement.Infrastructure.Observability;

namespace TicketManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Redis connection
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        // Distributed Lock
        services.AddSingleton<IDistributedLock, RedisDistributedLock>();

        // Observability
        services.AddSingleton<MetricsCollector>();
        services.AddSingleton<LockContentionMonitor>();

        return services;
    }
}

