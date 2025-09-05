using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.JSInterop;

namespace BlazorInvoice.Pwa.Services;

public class BackupService(IJSRuntime _js) : IBackupService
{
    private readonly Lock _lock = new();
    public async Task<BackupResult> Backup(string dir)
    {
        try
        {
            await _js.InvokeVoidAsync("downloadBackup");
            return new() { Success = true };
        }
        catch (Exception ex)
        {
            return new() { Error = ex.Message };
        }
    }

    public BackupResult Restore(string backupFile = "")
    {
        try
        {
            _js.InvokeVoidAsync("uploadBackup", true).GetAwaiter().GetResult();
            return new() { Success = true };
        }
        catch (Exception ex)
        {
            return new() { Error = ex.Message };
        }
    }
}


