using Microsoft.Extensions.Logging;
using Windows.Management.Deployment;
using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;

namespace BlazorInvoice.Maui.Services;

public class GitHubUpdateService(ILogger<GitHubUpdateService> logger) : IUpdateService
{
    private readonly string packageUri = "https://github.com/ipax77/beinx/releases/latest/download";
    private readonly string packageName = "BlazorInvoice.Maui";
    private Version latestVersion = new(0, 0, 0, 0);

    public event EventHandler<UpdateProgressEventArgs>? UpdateProgress;

    private void OnUpdateProgress(UpdateProgressEventArgs e)
    {
        var handler = UpdateProgress;
        handler?.Invoke(this, e);
    }

    public async Task<bool> CheckForUpdates()
    {
        Version currentVersion = GetCurrentVersion();

        HttpClient httpClient = new()
        {
            BaseAddress = new Uri(packageUri)
        };

        try
        {
            var stream = await httpClient.GetStreamAsync("/latest.yml");

            StreamReader reader = new StreamReader(stream);
            string versionInfo = await reader.ReadLineAsync() ?? "";


            if (Version.TryParse(versionInfo.Split(' ').LastOrDefault(), out Version? newVersion)
                && newVersion is not null)
            {
                latestVersion = newVersion;
                return latestVersion > currentVersion;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("latest version check failed: {error}", ex.Message);
        }
        return false;
    }

    public async Task<bool> UpdateApp()
    {
        if (latestVersion <= GetCurrentVersion())
        {
            return true;
        }

        try
        {
            PackageManager packageManager = new();
            var progress = new Progress<DeploymentProgress>(report =>
                OnUpdateProgress(new() { Progress = (int)report.percentage }));

            await packageManager.AddPackageAsync
            (
                new Uri($"{packageUri}/{packageName}_{latestVersion}_x64.msix"),
                null,
                DeploymentOptions.ForceApplicationShutdown
            )
            .AsTask(progress);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError("app update failed: {error}", ex.Message);
        }
        return false;
    }

    public Version GetCurrentVersion()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        return version ?? new Version(1, 0, 0, 1);
    }
}

