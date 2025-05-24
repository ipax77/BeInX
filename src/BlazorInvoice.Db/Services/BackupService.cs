using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;

namespace BlazorInvoice.Db.Services;

public class BackupService(IServiceScopeFactory scopeFactory) : IBackupService
{
    private readonly Lock _lock = new();
    public async Task<BackupResult> Backup(string dir)
    {
        try
        {
            using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<InvoiceContext>();
            var pathService = scope.ServiceProvider.GetRequiredService<IMauiPathService>();
            var dbFile = pathService.GetDbFileName();
            var dbBackupFile = await CreateBackupDb(dbFile);
            lock (_lock)
            {

                if (!File.Exists(dbBackupFile))
                {
                    return new() { Error = "Database file not found." };
                }
                if (!Directory.Exists(dir))
                {
                    return new() { Error = "Destination directory does not exists." };
                }
                var zipFileName = Path.Combine(dir, $"BeInX_Backup_{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")}.zip");
                if (File.Exists(zipFileName))
                {
                    return new() { Error = $"Destination file {zipFileName} already exists." };
                }
                var tempDir = Path.Combine(Path.GetTempPath(), "BeInX_Backup");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                var backupFile = Path.Combine(tempDir, Path.GetFileName(dbFile));
                File.Copy(dbBackupFile, backupFile, true);
                ZipFile.CreateFromDirectory(tempDir, zipFileName);
                if (File.Exists(zipFileName))
                {
                    File.Delete(backupFile);
                    Directory.Delete(tempDir);
                }
            }
            var config = await context.AppConfigs
                .OrderBy(o => o.AppConfigId)
                .FirstOrDefaultAsync();
            if (config != null)
            {
                config.LastBackup = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }

            return new() { Success = true };
        }
        catch (Exception ex)
        {
            return new() { Error = ex.Message };
        }
    }

    public BackupResult Restore(string backupFile)
    {
        lock (_lock)
        {
            try
            {
                using var scope = scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<InvoiceContext>();

                var pathService = scope.ServiceProvider.GetRequiredService<IMauiPathService>();
                var dbFile = pathService.GetDbFileName();
                if (!File.Exists(backupFile))
                {
                    return new() { Error = "Backup file not found." };
                }
                if (File.Exists(dbFile))
                {
                    File.Copy(dbFile, $"{dbFile}.bak2", true);
                }
                var configService = scope.ServiceProvider.GetRequiredService<IConfigService>();

                context.Database.EnsureDeleted();
                var dest = Path.GetDirectoryName(dbFile)
                    ?? throw new InvalidOperationException("Could not determine destination folder.");
                ZipFile.ExtractToDirectory(backupFile, dest, true);
                context.Database.Migrate();
                configService.Reload();
                return new() { Success = true };
            }
            catch (Exception ex)
            {
                return new() { Error = ex.Message };
            }
        }
    }

    private async Task<string?> CreateBackupDb(string dbFile)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceContext>();
        var backupFile = $"{dbFile}.bak";

        await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);");

        var backupConnectionString = $"Data Source={backupFile}";
        using var backupConnection = new SqliteConnection(backupConnectionString);
        using var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        if (connection is SqliteConnection sqliteConnection)
        {
            sqliteConnection.BackupDatabase(backupConnection);
        }
        else
        {
            throw new InvalidOperationException("No SqLite connection.");
        }
        return backupFile;
    }
}


