using BlazorInvoice.Localization;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace BlazorInvoice.Weblib;

public class InvoiceBaseComponent : ComponentBase, IDisposable
{
    [Inject]
    public IConfigService ConfigService { get; set; } = null!;

    [Inject]
    public IStringLocalizer<InvoiceLoc> Loc { get; set; } = null!;

    protected override void OnInitialized()
    {
        ConfigService.OnUpdate += UpdateState;
        base.OnInitialized();
    }

    public virtual void UpdateState(object? sender)
    {
        InvokeAsync(StateHasChanged);
    }

    public virtual void Dispose()
    {
        ConfigService.OnUpdate -= UpdateState;
    }
}
