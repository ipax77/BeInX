using Microsoft.Extensions.DependencyInjection;

namespace BlazorInvoice.Pdf;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPdfGenerator(this IServiceCollection services)
    {
        services.AddScoped<PdfJsInterop>();
        return services;
    }
}
