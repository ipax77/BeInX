using BlazorInvoice.Shared.Interfaces;
using CommunityToolkit.Maui.Storage;
using pax.BBToast;

namespace BlazorInvoice.Maui.Services;

public class MauiPathService(IToastService ToastService, IFolderPicker FolderPicker) : IMauiPathService
{
    public async Task<string?> PickFolder()
    {
        try
        {
            var result = await FolderPicker.PickAsync();
            result.EnsureSuccess();
            return result.Folder.Path;
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }

        return null;
    }

    public async Task<string?> PickFile()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync();
            return result?.FullPath;
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }

        return null;
    }

    public string GetDbFileName()
    {
        return MauiProgram.DbFile;
    }

    public string GetAppFolder()
    {
        return FileSystem.Current.AppDataDirectory;
    }

    public string GetDesktopFolder()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }
}
