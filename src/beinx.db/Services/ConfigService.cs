using beinx.shared;
using beinx.shared.Interfaces;
using System.Globalization;

namespace beinx.db.Services;

public class ConfigService(IIndexedDbInterop _interop) : IConfigService
{
    public event Action<AppConfigDto>? OnUpdate;
    private AppConfigDto? _appConfig;
    private readonly SemaphoreSlim ss = new(1, 1);

    public async Task<AppConfigDto> GetConfig()
    {
        await ss.WaitAsync();
        try
        {
            if (_appConfig is not null)
            {
                return _appConfig;
            }
            _appConfig = await _interop.CallAsync<AppConfigDto>("getConfig");
            return _appConfig ?? new AppConfigDto();
        }
        finally
        {
            ss.Release();
        }
    }

    public List<CultureInfo> GetSupportedCultures()
    {
        return [
            new CultureInfo("en"),
            new CultureInfo("es"),
            new CultureInfo("de"),
            new CultureInfo("fr"),
        ];
    }

    public Task<bool> IsBackupNeeded()
    {
        throw new NotImplementedException();
    }

    public Task Reload()
    {
        throw new NotImplementedException();
    }

    public async Task UpdateConfig(AppConfigDto configDto)
    {
        await ss.WaitAsync();
        try
        {
            await _interop.CallVoidAsync("saveConfig", configDto);
            _appConfig = configDto;
            OnUpdate?.Invoke(_appConfig);
        }
        finally
        {
            ss.Release();
        }
    }

    public async Task DownloadBackup()
    {
        await ss.WaitAsync();
        try
        {
            await _interop.CallVoidAsync("downloadBackup");
            if (_appConfig is null)
            {
                _appConfig = await _interop.CallAsync<AppConfigDto>("getConfig") ?? new();
            }
            _appConfig.LastBackup = DateTime.UtcNow;
            await _interop.CallVoidAsync("saveConfig", _appConfig);
            OnUpdate?.Invoke(_appConfig);
        }
        finally
        {
            ss.Release();
        }
    }

    public async Task RestoreBackup()
    {
        await ss.WaitAsync();
        try
        {
            await _interop.CallVoidAsync("uploadBackup");
            _appConfig = await _interop.CallAsync<AppConfigDto>("getConfig");
            _appConfig.LastBackup = DateTime.UtcNow;
            await _interop.CallVoidAsync("saveConfig", _appConfig);
        }
        finally
        {
            ss.Release();
        }
    }
}
