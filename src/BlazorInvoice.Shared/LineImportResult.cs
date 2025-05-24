using pax.XRechnung.NET.AnnotatedDtos;

namespace BlazorInvoice.Shared;

public class LineImportResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<InvoiceLineAnnotationDto> InvoiceLines { get; set; } = [];

    public static LineImportResult Success(List<InvoiceLineAnnotationDto> invoiceLines) =>
        new LineImportResult { IsSuccess = true, InvoiceLines = invoiceLines };

    public static LineImportResult Failure(string message) => new LineImportResult { IsSuccess = false, Message = message };
}
