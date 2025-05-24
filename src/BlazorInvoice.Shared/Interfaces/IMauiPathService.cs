
namespace BlazorInvoice.Shared.Interfaces;

public interface IMauiPathService
{
    string GetAppFolder();
    string GetDbFileName();
    string GetDesktopFolder();
    Task<string?> PickFile();
    Task<string?> PickFolder();
}