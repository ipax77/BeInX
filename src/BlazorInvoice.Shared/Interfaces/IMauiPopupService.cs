
namespace BlazorInvoice.Shared.Interfaces;

public interface IMauiPopupService
{
    Task DisplayAlert(string title, string message, string cancel);
    Task<bool> DisplayAlert(string title, string message, string accept, string cancel);
}