namespace pax.BBToast;

public class ToastService : IToastService
{
    public event Action<ToastOptions>? OnShow;

    public void ShowToast(ToastOptions options)
    {
        OnShow?.Invoke(options);
    }

    public void ShowInfo(string message, string title = "Info", string smallTitle = "") =>
        ShowToast(new ToastOptions { Message = message, Title = title, Type = ToastType.Info, SmallTitle = smallTitle });

    public void ShowSuccess(string message, string title = "Success", string smallTitle = "") =>
        ShowToast(new ToastOptions { Message = message, Title = title, Type = ToastType.Success, SmallTitle = smallTitle });

    public void ShowWarning(string message, string title = "Warning", string smallTitle = "") =>
        ShowToast(new ToastOptions { Message = message, Title = title, Type = ToastType.Warning, SmallTitle = smallTitle });

    public void ShowError(string message, string title = "Error", string smallTitle = "") =>
        ShowToast(new ToastOptions { Message = message, Title = title, Type = ToastType.Error, SmallTitle = smallTitle });
}
