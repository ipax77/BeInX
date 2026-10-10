using beinx.shared;
using beinx.shared.Interfaces;
using BlazorInvoice.Pdf;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace beinx.Tests;

// The renderer API is used only by this test harness.
#pragma warning disable BL0006
[TestClass]
public class PdfPreviewTests
{
    [TestMethod]
    public async Task ReplacedAndClearedPreviewsReleaseTheirUrls()
    {
        await using var preview = await Preview.CreateAsync();
        await preview.UpdateAsync();
        await preview.CompleteAsync(0, "blob:first");
        await preview.UpdateAsync();
        await preview.CompleteAsync(1, "blob:second");
        Assert.AreEqual("blob:second", preview.Component.ObjectUrl);
        CollectionAssert.AreEqual(new[] { "blob:first" }, preview.JS.Released);
        await preview.Renderer.Dispatcher.InvokeAsync(() => preview.Component.Update(null));
        Assert.AreEqual(string.Empty, preview.Component.ObjectUrl);
        CollectionAssert.AreEqual(new[] { "blob:first", "blob:second" }, preview.JS.Released);
    }

    [TestMethod]
    public async Task OlderResultCannotReplaceNewerPreview()
    {
        await using var preview = await Preview.CreateAsync();
        await preview.UpdateAsync();
        await preview.UpdateAsync();
        await preview.CompleteAsync(1, "blob:new");
        await preview.CompleteAsync(0, "blob:old");
        Assert.AreEqual("blob:new", preview.Component.ObjectUrl);
        CollectionAssert.AreEqual(new[] { "blob:old" }, preview.JS.Released);
    }

    [TestMethod]
    public async Task DisposalReleasesCurrentAndPendingPreviewUrls()
    {
        await using var preview = await Preview.CreateAsync();
        await preview.UpdateAsync();
        await preview.CompleteAsync(0, "blob:current");
        await preview.UpdateAsync();
        await preview.Renderer.Dispatcher.InvokeAsync(() => preview.Component.DisposeAsync().AsTask());
        await preview.CompleteAsync(1, "blob:pending");
        await preview.UpdateAsync();
        Assert.HasCount(2, preview.Pdf.Pending);
        Assert.AreEqual(string.Empty, preview.Component.ObjectUrl);
        CollectionAssert.AreEqual(new[] { "blob:current", "blob:pending" }, preview.JS.Released);
    }

    [TestMethod]
    public async Task StaleFailureKeepsNewPreviewAndCurrentFailureReleasesIt()
    {
        await using var preview = await Preview.CreateAsync();
        await preview.UpdateAsync();
        await preview.UpdateAsync();
        await preview.CompleteAsync(1, "blob:new");
        await preview.Renderer.Dispatcher.InvokeAsync(() => preview.Pdf.Pending[0].SetException(new Exception("stale error")));
        Assert.AreEqual("blob:new", preview.Component.ObjectUrl);
        await preview.UpdateAsync();
        await preview.Renderer.Dispatcher.InvokeAsync(() => preview.Pdf.Pending[2].SetException(new Exception("current error")));
        Assert.AreEqual(string.Empty, preview.Component.ObjectUrl);
        CollectionAssert.AreEqual(new[] { "blob:new" }, preview.JS.Released);
    }

    private sealed class Preview : IAsyncDisposable
    {
        public FakePdf Pdf { get; } = new();
        public FakeJS JS { get; } = new();
        public TestRenderer Renderer { get; private set; } = null!;
        public InvoicePdfComponent Component { get; private set; } = null!;
        private ServiceProvider services = null!;

        public static async Task<Preview> CreateAsync()
        {
            var preview = new Preview();
            preview.services = new ServiceCollection().AddSingleton<IPdfJsInterop>(preview.Pdf)
                .AddSingleton<IJSRuntime>(preview.JS).BuildServiceProvider();
            preview.Renderer = new TestRenderer(preview.services);
            await preview.Renderer.Dispatcher.InvokeAsync(async () =>
            {
                preview.Component = preview.Renderer.CreateComponent();
                await preview.Renderer.MountAsync(preview.Component);
            });
            return preview;
        }

        public Task UpdateAsync() => Renderer.Dispatcher.InvokeAsync(() => Component.Update(new BlazorInvoiceDto()));
        public Task CompleteAsync(int index, string url) => Renderer.Dispatcher.InvokeAsync(() => Pdf.Pending[index].SetResult(url));
        public async ValueTask DisposeAsync()
        {
            await Renderer.DisposeAsync();
            await services.DisposeAsync();
        }
    }

    // Use the framework renderer to exercise the actual component without a browser.
    private sealed class TestRenderer(IServiceProvider services) : Renderer(services, NullLoggerFactory.Instance)
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
        public InvoicePdfComponent CreateComponent() => (InvoicePdfComponent)InstantiateComponent(typeof(InvoicePdfComponent));
        public Task MountAsync(InvoicePdfComponent component) => RenderRootComponentAsync(AssignRootComponentId(component), ParameterView.Empty);
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
        protected override void HandleException(Exception exception) => throw exception;
    }

    private sealed class FakePdf : IPdfJsInterop
    {
        public List<TaskCompletionSource<string>> Pending { get; } = [];
        public ValueTask<string> CreateInvoicePdf(BlazorInvoiceDto invoice, string cultureName)
        {
            var completion = new TaskCompletionSource<string>();
            Pending.Add(completion);
            return new ValueTask<string>(completion.Task);
        }
        public ValueTask<string> CreateInvoicePdfA3(BlazorInvoiceDto invoice, string cultureName, string hexId, string xmlText) => throw new NotSupportedException();
        public ValueTask<byte[]> CreateInvoicePdfA3Bytes(BlazorInvoiceDto invoice, string cultureName, string hexId, string xmlText) => throw new NotSupportedException();
        public ValueTask<byte[]> CreateInvoicePdfBytes(BlazorInvoiceDto invoice, string cultureName) => throw new NotSupportedException();
        public ValueTask<string?> GetXmlString(byte[] pdfBytes) => throw new NotSupportedException();
    }

    private sealed class FakeJS : IJSRuntime
    {
        public List<string> Released { get; } = [];
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Assert.AreEqual("URL.revokeObjectURL", identifier);
            Released.Add((string)args![0]!);
            return ValueTask.FromResult(default(TValue)!);
        }
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }
}
#pragma warning restore BL0006
