using BlazorInvoice.Shared;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BlazorInvoice.Db;

public class AppConfig
{
    public int AppConfigId { get; set; }
    public Guid Guid { get; set; } = Guid.NewGuid();
    public DateTime LastBackup { get; set; }
    public string BackupFolder { get; set; } = string.Empty;
    public BackupInterval BackupInterval { get; set; }
    public string CultureName { get; set; } = string.Empty;
    [MaxLength(200)]
    public string SchematronValidationUri { get; set; } = string.Empty;
    [Precision(0)]
    public bool ShowFormDescriptions { get; set; } = true;
    public bool ShowValidationWarnings { get; set; }

    // Export
    public bool ExportEmbedPdf { get; set; } = true;
    public bool ExportValidate { get; set; } = true;
    public bool ExportFinalize { get; set; } = true;
    public ExportType ExportType { get; set; }

    // Stats
    public int StatsMonthEndDay { get; set; } = 10;
    public bool StatsIsMonthNotQuater { get; set; }
}
