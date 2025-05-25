using Microsoft.Extensions.Logging;
using Windows.Management.Deployment;
using BlazorInvoice.Shared;

namespace BlazorInvoice.Maui.Services;

public class GitHubUpdateService(ILogger<GitHubUpdateService> logger)
{
    private readonly string packageUri = "https://github.com/ipax77/beinx/releases/latest/download";
    private readonly string packageName = "BlazorInvoice.Maui";
    public static readonly Version CurrentVersion = new(1, 0, 0, 0);
    private Version latestVersion = new(0, 0, 0, 0);

    public EventHandler<UpdateProgressEventArgs>? UpdateProgress;

    private void OnUpdateProgress(UpdateProgressEventArgs e)
    {
        var handler = UpdateProgress;
        handler?.Invoke(this, e);
    }

    public async Task<bool> CheckForUpdates()
    {
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
                return latestVersion > CurrentVersion;
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
        if (latestVersion <= CurrentVersion)
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

}

