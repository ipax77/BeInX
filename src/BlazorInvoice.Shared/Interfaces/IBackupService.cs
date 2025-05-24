namespace BlazorInvoice.Shared.Interfaces;

public interface IBackupService
{
    Task<BackupResult> Backup(string dir);
    BackupResult Restore(string backupFile);
}