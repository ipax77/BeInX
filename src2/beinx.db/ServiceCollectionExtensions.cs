using beinx.db.Services;
using beinx.shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace beinx.db;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBeinxDbServices(this IServiceCollection services)
    {
        services.AddScoped<IConfigService, ConfigService>();
        services.AddSingleton<IIndexedDbService, IndexedDbService>();
        return services;
    }
}
