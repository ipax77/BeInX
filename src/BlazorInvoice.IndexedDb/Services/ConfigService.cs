
using System.Globalization;
using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;

namespace BlazorInvoice.IndexedDb.Services;

public class ConfigService(IIndexedDbService indexedDbService) : IConfigService
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
            _appConfig = await indexedDbService.GetConfig();
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

    public async Task<bool> IsBackupNeeded()
    {
        if (_appConfig == null)
        {
            return false;
        }
        if (_appConfig.BackupInterval == BackupInterval.None)
        {
            return false;
        }
        if (_appConfig.BackupInterval == BackupInterval.OnClose)
        {
            return true;
        }
        var lastBackup = _appConfig.LastBackup;
        if (lastBackup == DateTime.MinValue)
        {
            return true;
        }
        var daysSinceLastBackup = (DateTime.Today - lastBackup).TotalDays;
        if (_appConfig.BackupInterval == BackupInterval.Every30Days && daysSinceLastBackup >= 30)
        {
            return true;
        }
        if (_appConfig.BackupInterval == BackupInterval.Every90Days && daysSinceLastBackup >= 90)
        {
            return true;
        }
        return await Task.FromResult(false);
    }

    public async Task Reload()
    {
        var config = await indexedDbService.GetConfig();
        if (config == null)
        {
            config = new();
            await indexedDbService.SaveConfig(config);
        }
        _appConfig = config;
    }

    public async Task UpdateConfig(AppConfigDto configDto)
    {
        await ss.WaitAsync();
        try
        {
            await indexedDbService.SaveConfig(configDto);
            _appConfig = configDto;
            OnUpdate?.Invoke(_appConfig);
        }
        finally
        {
            ss.Release();
        }
    }
}