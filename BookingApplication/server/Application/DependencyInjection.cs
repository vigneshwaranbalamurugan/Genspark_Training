using Microsoft.Extensions.DependencyInjection;
using server.Application.Services.Implementations;
using server.Application.Services.Interfaces;

namespace server.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IOperatorService, OperatorService>();
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IRegistrationService, RegistrationService>();

        return services;
    }
}
