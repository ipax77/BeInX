using beinx.shared;
using beinx.shared.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorInvoice.Pdf
{
    public class PdfJsInterop : IAsyncDisposable, IPdfJsInterop
    {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask;
        private static readonly HashSet<string> SupportedCultures = ["en", "fr", "es", "de"];

        public PdfJsInterop(IJSRuntime jsRuntime, NavigationManager nav)
        {
            var baseUri = nav.BaseUri;
            moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", $"{baseUri}_content/BlazorInvoice.Pdf/dist/pdf-generator.js").AsTask());
        }

        public async ValueTask<string> CreateInvoicePdf(BlazorInvoiceDto invoice, string cultureName)
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<string>("createInvoicePdf", invoice, GetCultureName(cultureName));
        }

        public async ValueTask<byte[]> CreateInvoicePdfBytes(BlazorInvoiceDto invoice, string cultureName)
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<byte[]>("createInvoicePdfBytes", invoice, GetCultureName(cultureName));
        }

        public async ValueTask<string> CreateInvoicePdfA3(BlazorInvoiceDto invoice, string cultureName, string hexId, string xmlText)
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<string>("createInvoicePdfA3", invoice, GetCultureName(cultureName), hexId, xmlText);
        }

        public async ValueTask<byte[]> CreateInvoicePdfA3Bytes(BlazorInvoiceDto invoice, string cultureName, string hexId, string xmlText)
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<byte[]>("createInvoicePdfA3Bytes", invoice, GetCultureName(cultureName), hexId, xmlText);
        }

        public async ValueTask<string?> GetXmlString(byte[] pdfBytes)
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<string?>("getPdfXmlText", pdfBytes);
        }

        private static string GetCultureName(string cultureName)
        {
            var baseCulture = cultureName.Split('-')[0];
            return SupportedCultures.Contains(baseCulture) ? baseCulture : "en";
        }

        public async ValueTask DisposeAsync()
        {
            if (moduleTask.IsValueCreated)
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}
