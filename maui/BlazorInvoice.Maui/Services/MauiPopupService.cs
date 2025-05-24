using BlazorInvoice.Shared.Interfaces;

namespace BlazorInvoice.Maui.Services;

public class MauiPopupService : IMauiPopupService
{
    public async Task DisplayAlert(string title, string message, string cancel)
    {
        var page = Application.Current?.Windows[0].Page;
        if (page is not null)
        {
            await page.DisplayAlert(title, message, cancel);
        }
    }

    public async Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
    {
        var page = Application.Current?.Windows[0].Page;
        var result = false;
        if (page is not null)
        {
            result = await page.DisplayAlert(title, message, accept, cancel);
        }
        return result;
    }
}
