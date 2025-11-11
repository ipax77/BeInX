
namespace pax.BBToast
{
    public interface IToastService
    {
        event Action<ToastOptions>? OnShow;

        void ShowError(string message, string title = "Error", string smallTitle = "");
        void ShowInfo(string message, string title = "Info", string smallTitle = "");
        void ShowSuccess(string message, string title = "Success", string smallTitle = "");
        void ShowToast(ToastOptions options);
        void ShowWarning(string message, string title = "Warning", string smallTitle = "");
    }
}