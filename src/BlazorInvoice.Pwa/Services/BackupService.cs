using BlazorInvoice.IndexedDb.Services;
using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;

namespace BlazorInvoice.Pwa.Services;

public class BackupService(IIndexedDbService indexedDbService) : IBackupService
{
    private readonly Lock _lock = new();
    public async Task<BackupResult> Backup(string dir)
    {
        try
        {
            await indexedDbService.DownloadBackup();
            return new() { Success = true };
        }
        catch (Exception ex)
        {
            return new() { Error = ex.Message };
        }
    }

    public async Task<BackupResult> RestoreAsync(string backupFile = "")
    {
        try
        {
            await indexedDbService.UploadBackup();
            return new() { Success = true };
        }
        catch (Exception ex)
        {
            return new() { Error = ex.Message };
        }
    }

    public BackupResult Restore(string backupFile = "")
    {
        return RestoreAsync(backupFile).GetAwaiter().GetResult();
    }
}


