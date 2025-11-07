using beinx.shared;
using beinx.shared.Interfaces;
using System.Globalization;

namespace beinx.db.Services;

public class ConfigService : IConfigService
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
            // _appConfig = await indexedDbService.GetConfig();
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
            // await indexedDbService.SaveConfig(configDto);
            _appConfig = configDto;
            OnUpdate?.Invoke(_appConfig);
        }
        finally
        {
            ss.Release();
        }
    }
}
