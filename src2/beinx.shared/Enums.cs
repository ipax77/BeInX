namespace beinx.shared;


public enum BackupInterval
{
    None = 0,
    OnClose = 1,
    Every30Days = 2,
    Every90Days = 3,
}

public enum ExportType
{
    XmlAndPdf = 0,
    Xml = 1,
    Pdf = 2,
    PdfA3 = 3,
}

public static class ExportTypeExtensions
{
    public static string Desc(this ExportType type)
    {
        return type switch
        {
            ExportType.XmlAndPdf => "XML and PDF",
            ExportType.Xml => "XML only",
            ExportType.Pdf => "PDF only",
            ExportType.PdfA3 => "ZUGFeRD PDF",
            _ => "unknown"
        };
    }

    public static string LongDesc(this ExportType type)
    {
        return type switch
        {
            ExportType.XmlAndPdf => "Export both XML and PDF as zip file.",
            ExportType.Xml => "Export only XML file.",
            ExportType.Pdf => "Export only PDF file.",
            ExportType.PdfA3 => "Export PDF with embedded XML for ZUGFeRD compatibility (PDF/A3b).",
            _ => "unknown"
        };
    }
}

public static class BackupIntervalExtensions
{
    public static string Desc(this BackupInterval interval)
    {
        return interval switch
        {
            BackupInterval.None => "No Backup",
            BackupInterval.OnClose => "On Close",
            BackupInterval.Every30Days => "Every 30 days",
            BackupInterval.Every90Days => "Every 90 days",
            _ => "unknown"
        };
    }
}
