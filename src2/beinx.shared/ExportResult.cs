namespace beinx.shared;

public sealed record FinalizeResult(DateTime Created, string Sha1Hash, byte[] Blob, string MimeType = "application/octetstream");

public record ExportResult(FinalizeResult? FinalizeResult, string FileName, string? Error);

public record ImportResult(InvoiceDtoInfo? Info, string? Error);