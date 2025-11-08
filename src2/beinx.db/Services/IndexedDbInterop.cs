using Microsoft.JSInterop;

namespace beinx.db.Services;

public class IndexedDbInterop : IDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    public IndexedDbInterop(IJSRuntime js)
    {
        _moduleTask = new(() => js.InvokeAsync<IJSObjectReference>(
            "import", "./_content/beinx.db/js/beinx-db.bundle.js").AsTask());
    }

    public async Task<T> CallAsync<T>(string method, params object[] args)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<T>(method, args);
    }

    public async Task CallVoidAsync(string method, params object[] args)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync(method, args);
    }

    public void Dispose()
    {
        if (_moduleTask.IsValueCreated)
        {
            _moduleTask.Value.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
