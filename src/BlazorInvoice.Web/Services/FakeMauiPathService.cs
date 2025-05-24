using BlazorInvoice.Shared.Interfaces;

namespace BlazorInvoice.Web.Services;

public class FakeMauiPathService(IConfiguration configuration) : IMauiPathService
{
    public string GetAppFolder()
    {
        return Path.GetDirectoryName(configuration["DbName"]) ?? "/data/xrechnung";
    }

    public string GetDbFileName()
    {
        return configuration["DbName"] ?? "/data/xrechnung/invoice.db";
    }

    public string GetDesktopFolder()
    {
        return Path.GetDirectoryName(configuration["DbName"]) ?? "/data/xrechnung";
    }

    public Task<string?> PickFile()
    {
        throw new NotImplementedException();
    }

    public Task<string?> PickFolder()
    {
        throw new NotImplementedException();
    }
}
