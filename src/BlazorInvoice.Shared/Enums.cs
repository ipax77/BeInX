
namespace BlazorInvoice.Shared;

public enum BackupInterval
{
    None = 0,
    OnClose = 1,
    Every30Days = 2,
    Every90Days = 3,
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