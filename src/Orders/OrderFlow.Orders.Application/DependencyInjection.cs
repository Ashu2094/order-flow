using Microsoft.Extensions.DependencyInjection;

namespace OrderFlow.Orders.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        return services;
    }
}