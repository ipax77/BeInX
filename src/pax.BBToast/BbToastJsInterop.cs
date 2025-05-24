using Microsoft.JSInterop;

namespace pax.BBToast
{
    public class BbToastJsInterop(IJSRuntime jsRuntime) : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/pax.BBToast/bbToastJsInterop.js").AsTask());

        public async ValueTask ShowToast(string id)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("showToast", id);
        }

        public async ValueTask HideToast(string id)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("hideToast", id);
        }

        public async ValueTask DisposeAsync()
        {
            if (moduleTask.IsValueCreated)
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}
