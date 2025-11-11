using Microsoft.Extensions.DependencyInjection;

namespace pax.BBToast;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBbToast(this IServiceCollection services)
    {
        services.AddScoped<IToastService, ToastService>();
        services.AddScoped<BbToastJsInterop>();
        return services;
    }
}
