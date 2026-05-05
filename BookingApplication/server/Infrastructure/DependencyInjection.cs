using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using server.Domain.Interfaces;
using server.Infrastructure.Data;
using server.Infrastructure.Repositories;

namespace server.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSql") 
            ?? throw new InvalidOperationException("Connection string 'PostgreSql' not found.");

        services.AddSingleton(new DbConnectionFactory(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOperatorRepository, OperatorRepository>();
        services.AddScoped<IBusRepository, BusRepository>();
        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ISeatLockRepository, SeatLockRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IPlatformFeeRepository, PlatformFeeRepository>();
        services.AddScoped<IPreferredRouteRepository, PreferredRouteRepository>();
        services.AddScoped<IRegistrationRepository, RegistrationRepository>();

        return services;
    }
}
