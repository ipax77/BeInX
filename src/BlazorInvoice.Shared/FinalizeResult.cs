namespace BlazorInvoice.Shared;

public sealed record FinalizeResult(DateTime Created, string Sha1Hash, byte[] Blob, string MimeType = "application/octetstream");

public record ExportResult(FinalizeResult FinalizeResult, string FileName, string? Error);

public enum ExportType
{
    XmlAndPdf = 0,
    Xml = 1,
    Pdf = 2,
}