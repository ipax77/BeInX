using beinx.db.Services;
using beinx.shared;
using beinx.shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using pax.XRechnung.NET.AnnotatedDtos;

namespace beinx.db;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBeinxDbServices(this IServiceCollection services)
    {
        services.AddSingleton<IIndexedDbInterop, IndexedDbInterop>();
        services.AddScoped<IConfigService, ConfigService>();
        services.AddScoped<IPaymentsRepository, PaymentsRepository>();
        services.AddScoped<IDraftRepository<PaymentAnnotationDto>>(sp =>
            (IDraftRepository<PaymentAnnotationDto>)sp.GetRequiredService<IPaymentsRepository>());

        return services;
    }
}
