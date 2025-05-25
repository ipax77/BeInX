namespace BlazorInvoice.Shared;

public record AppConfigDto
{
    public string CultureName { get; set; } = string.Empty;
    public string BackupFolder { get; set; } = string.Empty;
    public BackupInterval BackupInterval { get; set; }
    public string SchematronValidationUri { get; set; } = string.Empty;
    public bool ShowFormDescriptions { get; set; } = true;
    public bool ShowValidationWarnings { get; set; }
    public bool CheckForUpdates { get; set; } = true;
    public bool ExportEmbedPdf { get; set; } = true;
    public bool ExportValidate { get; set; } = true;
    public bool ExportFinalize { get; set; } = true;
    public ExportType ExportType { get; set; }
    public int StatsMonthEndDay { get; set; } = 10;
    public bool StatsIsMonthNotQuater { get; set; }
}
