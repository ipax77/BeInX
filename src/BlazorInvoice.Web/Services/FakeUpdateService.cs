using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;

namespace BlazorInvoice.Web.Services;

public class FakeUpdateService : IUpdateService
{
    public event EventHandler<UpdateProgressEventArgs>? UpdateProgress;

    private void OnUpdateProgress(UpdateProgressEventArgs e)
    {
        var handler = UpdateProgress;
        handler?.Invoke(this, e);
    }

    public Task<bool> CheckForUpdates()
    {
        return Task.FromResult(true);
    }

    public Version GetCurrentVersion()
    {
        return new(1, 0, 0, 0);
    }

    public Task<bool> UpdateApp()
    {
        OnUpdateProgress(new() { Progress = 24 });
        return Task.FromResult(true);
    }
}
