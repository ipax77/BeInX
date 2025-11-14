using pax.XRechnung.NET.AnnotatedDtos;

namespace beinx.shared;

public class LineImportResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<InvoiceLineAnnotationDto> InvoiceLines { get; set; } = [];

    public static LineImportResult Success(List<InvoiceLineAnnotationDto> invoiceLines) =>
        new LineImportResult { IsSuccess = true, InvoiceLines = invoiceLines };

    public static LineImportResult Failure(string message) => new() { IsSuccess = false, Message = message };
}