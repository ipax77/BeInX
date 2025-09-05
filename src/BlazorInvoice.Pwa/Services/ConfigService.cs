using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.JSInterop;
using System.Globalization;

namespace BlazorInvoice.Pwa.Services;

public class ConfigService : IConfigService
{
    private Task<IJSObjectReference> moduleTask;
    public ConfigService(IJSRuntime js)
    {
        moduleTask = js.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorInvoice.IndexedDb/js/beinx-db.js").AsTask();
    }

    private AppConfigDto? _appConfig;
    private readonly SemaphoreSlim ss = new(1, 1);

    public event Action<AppConfigDto>? OnUpdate;

    public async Task<AppConfigDto> GetConfig()
    {
        await ss.WaitAsync();
        try
        {
            if (_appConfig is not null)
                return _appConfig;
            await Reload();
            if (_appConfig is null)
            {
                _appConfig = new();
            }
            return _appConfig;
        }
        finally
        {
            ss.Release();
        }
    }

    public async Task Reload()
    {
        var module = await moduleTask;
        var config = await module.InvokeAsync<AppConfigDto>("getConfig");
        if (config is null)
        {
            config = new();
            await module.InvokeVoidAsync("saveConfig", config);
        }
        _appConfig = config;
    }

    public async Task UpdateConfig(AppConfigDto configDto)
    {
        await ss.WaitAsync();
        try
        {
            var module = await moduleTask;
            var config = await module.InvokeAsync<AppConfigDto>("getConfig");
            if (config is null)
            {
                config = new();
            }
            _appConfig = config;
            await module.InvokeVoidAsync("saveConfig", config);
            OnUpdate?.Invoke(configDto);
        }
        finally
        {
            ss.Release();
        }
    }

    public List<CultureInfo> SupportedCultures { get; } =
    [
        new CultureInfo("en"),
        new CultureInfo("es"),
        new CultureInfo("de"),
        new CultureInfo("fr"),
    ];

    public List<CultureInfo> GetSupportedCultures()
    {
        return SupportedCultures;
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
}
