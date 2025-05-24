using BlazorInvoice.Db;
using BlazorInvoice.Db.Repository;
using BlazorInvoice.Db.Services;
using BlazorInvoice.Maui.Services;
using BlazorInvoice.Pdf;
using BlazorInvoice.Shared.Interfaces;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.BBToast;
using pax.BlazorChartJs;
using System.Globalization;

namespace BlazorInvoice.Maui
{
    public static class MauiProgram
    {
        public static readonly string DbFile = Path.Combine(FileSystem.Current.AppDataDirectory, "beinx.db");
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkitCore()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            var sqliteConnectionString = $"Data Source={DbFile}";
            // Add services to the container.
            builder.Services.AddDbContext<InvoiceContext>(options => options
                .UseSqlite(sqliteConnectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("BlazorInvoice.Db");
                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                })
            );
            builder.Services.AddSingleton<IFolderPicker>(FolderPicker.Default);
            builder.Services.AddSingleton<IConfigService, ConfigService>();
            builder.Services.AddSingleton<IBackupService, BackupService>();

            builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            builder.Services.AddScoped<IMauiPathService, MauiPathService>();
            builder.Services.AddScoped<IStatsRepository, StatsRepository>();
            builder.Services.AddScoped<IMauiPopupService, MauiPopupService>();

            builder.Services.AddTransient<MainPage>();

            builder.Services.AddPdfGenerator();
            builder.Services.AddBbToast();
            builder.Services.AddLocalization();
            builder.Services.AddChartJs(options =>
            {
                options.ChartJsLocation = "/_content/BlazorInvoice.Weblib/js/chart.umd.js";
                options.ChartJsPluginDatalabelsLocation = "/_content/BlazorInvoice.Weblib/js/chartjs-plugin-datalabels.js";
            });

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<InvoiceContext>();
            // context.Database.EnsureDeleted();
            context.Database.Migrate();

            var configService = app.Services.GetRequiredService<IConfigService>();
            var config = configService.GetConfig().GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(config.CultureName))
            {
                var cultureInfo = new CultureInfo(config.CultureName);
                CultureInfo.CurrentCulture = cultureInfo;
                CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            }
            return app;
        }
    }
}
