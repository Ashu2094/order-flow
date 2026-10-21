using Microsoft.Extensions.DependencyInjection;

namespace OrderFlow.Payments.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        return services;
    }
}