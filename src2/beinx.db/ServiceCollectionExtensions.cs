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

        // Seller repository and draft
        services.AddScoped<ISellerRepository, SellerRepository>();
        services.AddScoped<IDraftRepository<SellerAnnotationDto>>(sp =>
            (IDraftRepository<SellerAnnotationDto>)sp.GetRequiredService<ISellerRepository>());

        // Buyer repository and draft
        services.AddScoped<IBuyerRepository, BuyerRepository>();
        services.AddScoped<IDraftRepository<BuyerAnnotationDto>>(sp =>
            (IDraftRepository<BuyerAnnotationDto>)sp.GetRequiredService<IBuyerRepository>());
        return services;
    }
}
