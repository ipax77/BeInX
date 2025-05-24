using BlazorInvoice.Shared.Interfaces;

namespace BlazorInvoice.Web.Services;

public class FakeMauiPopupService : IMauiPopupService
{
    public Task DisplayAlert(string title, string message, string cancel)
    {
        return Task.CompletedTask;
    }

    public Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
    {
        return Task.FromResult(true);
    }
}
