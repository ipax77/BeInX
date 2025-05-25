using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace BlazorInvoice.Db.Services;

public class ConfigService(IServiceScopeFactory scopeFactory) : IConfigService
{
    private AppConfig? _appConfig;
    private readonly SemaphoreSlim ss = new(1, 1);

    public event Action<AppConfigDto>? OnUpdate;

    public async Task<AppConfigDto> GetConfig()
    {
        await ss.WaitAsync();
        try
        {
            if (_appConfig is not null)
                return MapConfig(_appConfig);
            await Reload();
            if (_appConfig is null)
            {
                _appConfig = new();
            }
            return MapConfig(_appConfig);
        }
        finally
        {
            ss.Release();
        }
    }

    public async Task Reload()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceContext>();
        var config = await context.AppConfigs
            .OrderBy(o => o.AppConfigId)
            .FirstOrDefaultAsync();
        if (config is null)
        {
            config = new AppConfig();
            context.AppConfigs.Add(config);
            await context.SaveChangesAsync();
        }
        _appConfig = config;
    }

    public async Task UpdateConfig(AppConfigDto configDto)
    {
        await ss.WaitAsync();
        try
        {
            using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<InvoiceContext>();
            var config = await context.AppConfigs
                .OrderBy(o => o.AppConfigId)
                .FirstOrDefaultAsync();
            if (config is null)
            {
                config = new AppConfig();
                context.AppConfigs.Add(config);
            }
            MapConfig(configDto, config);
            await context.SaveChangesAsync();
            _appConfig = config;
            OnUpdate?.Invoke(configDto);
        }
        finally
        {
            ss.Release();
        }
    }

    private static void MapConfig(AppConfigDto configDto, AppConfig config)
    {
        config.CultureName = configDto.CultureName;
        config.SchematronValidationUri = configDto.SchematronValidationUri;
        config.ShowFormDescriptions = configDto.ShowFormDescriptions;
        config.ShowValidationWarnings = configDto.ShowValidationWarnings;
        config.CheckForUpdates = configDto.CheckForUpdates;
        config.ExportEmbedPdf = configDto.ExportEmbedPdf;
        config.ExportValidate = configDto.ExportValidate;
        config.ExportFinalize = configDto.ExportFinalize;
        config.ExportType = configDto.ExportType;
        config.BackupFolder = configDto.BackupFolder;
        config.BackupInterval = configDto.BackupInterval;
        config.StatsIsMonthNotQuater = configDto.StatsIsMonthNotQuater;
        config.StatsMonthEndDay = configDto.StatsMonthEndDay;
    }

    private static AppConfigDto MapConfig(AppConfig config)
    {
        return new AppConfigDto
        {
            CultureName = config.CultureName,
            SchematronValidationUri = config.SchematronValidationUri,
            ShowFormDescriptions = config.ShowFormDescriptions,
            ShowValidationWarnings = config.ShowValidationWarnings,
            CheckForUpdates = config.CheckForUpdates,
            ExportValidate = config.ExportValidate,
            ExportEmbedPdf = config.ExportEmbedPdf,
            ExportFinalize = config.ExportFinalize,
            ExportType = config.ExportType,
            BackupFolder = config.BackupFolder,
            BackupInterval = config.BackupInterval,
            StatsIsMonthNotQuater = config.StatsIsMonthNotQuater,
            StatsMonthEndDay = config.StatsMonthEndDay,

        };
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
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceContext>();
        var lastBackup = await context.AppConfigs
            .OrderBy(o => o.AppConfigId)
            .Select(s => s.LastBackup)
            .FirstOrDefaultAsync();
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
        return false;
    }
}
