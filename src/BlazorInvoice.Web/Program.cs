using BlazorInvoice.Db;
using BlazorInvoice.Db.Repository;
using BlazorInvoice.Db.Services;
using BlazorInvoice.Shared.Interfaces;
using BlazorInvoice.Web.Components;
using BlazorInvoice.Pdf;
using pax.BBToast;
using Microsoft.EntityFrameworkCore;
using BlazorInvoice.Web.Services;
using pax.BlazorChartJs;
using System.Globalization;


namespace BlazorInvoice.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var dbfile = builder.Configuration["DbFile"];
            var sqliteConnectionString = $"Data Source={dbfile}";
            // Add services to the container.
            builder.Services.AddDbContext<InvoiceContext>(options => options
                .UseSqlite(sqliteConnectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("BlazorInvoice.Db");
                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                })
            );

            builder.Services.AddSingleton<IConfigService, ConfigService>();
            builder.Services.AddSingleton<IBackupService, BackupService>();

            builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            builder.Services.AddScoped<IStatsRepository, StatsRepository>();
            builder.Services.AddScoped<IMauiPathService, FakeMauiPathService>();
            builder.Services.AddScoped<IMauiPopupService, FakeMauiPopupService>();

            builder.Services.AddPdfGenerator();
            builder.Services.AddBbToast();
            builder.Services.AddLocalization();
            builder.Services.AddChartJs(options =>
            {
                options.ChartJsLocation = "/_content/BlazorInvoice.Weblib/js/chart.umd.js";
                options.ChartJsPluginDatalabelsLocation = "/_content/BlazorInvoice.Weblib/js/chartjs-plugin-datalabels.js";
            });

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();

            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<InvoiceContext>();
            // context.Database.EnsureDeleted();
            context.Database.Migrate();

            var configService = scope.ServiceProvider.GetRequiredService<IConfigService>();
            var config = configService.GetConfig().GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(config.CultureName))
            {
                var culture = new CultureInfo(config.CultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            else
            {
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
