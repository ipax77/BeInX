
namespace BlazorInvoice.Shared.Interfaces;

public interface IUpdateService
{
    event EventHandler<UpdateProgressEventArgs>? UpdateProgress;
    Task<bool> CheckForUpdates();
    Task<bool> UpdateApp();
    Version GetCurrentVersion();
}