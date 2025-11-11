namespace beinx.shared.Interfaces;

public interface IPdfJsInterop
{
    ValueTask<string> CreateInvoicePdf(BlazorInvoiceDto invoice, string cultureName);
    ValueTask<string> CreateInvoicePdfA3(BlazorInvoiceDto invoice, string cultureName, string hexId, string xmlText);
    ValueTask<byte[]> CreateInvoicePdfA3Bytes(BlazorInvoiceDto invoice, string cultureName, string hexId, string xmlText);
    ValueTask<byte[]> CreateInvoicePdfBytes(BlazorInvoiceDto invoice, string cultureName);
}