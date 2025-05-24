using System.Globalization;

namespace BlazorInvoice.Shared.Interfaces;

public interface IConfigService
{
    Task<AppConfigDto> GetConfig();
    Task UpdateConfig(AppConfigDto configDto);
    List<CultureInfo> GetSupportedCultures();
    event Action<AppConfigDto>? OnUpdate;
    Task Reload();
    Task<bool> IsBackupNeeded();
}