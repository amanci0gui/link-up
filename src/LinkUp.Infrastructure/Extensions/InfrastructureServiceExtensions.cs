using LinkUp.Application.Common.Interfaces;
using LinkUp.Infrastructure.Cache;
using LinkUp.Infrastructure.Persistence.Repositories;
using LinkUp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace LinkUp.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // PostgreSQL connection string
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Repositories
        services.AddScoped<LinkUp.Domain.Interfaces.Repositories.IUserRepository>(
            _ => new UserRepository(connectionString));

        // Redis
        var redisConnection = config.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' not found.");
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<ICacheService, RedisCacheService>();

        // Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
