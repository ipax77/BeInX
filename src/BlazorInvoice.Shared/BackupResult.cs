namespace BlazorInvoice.Shared;

public sealed record BackupResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
}
