using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorInvoice.Pwa;
using BlazorInvoice.Shared.Interfaces;
using BlazorInvoice.IndexedDb.Services;
using BlazorInvoice.Pdf;
using pax.BBToast;
using pax.BlazorChartJs;
using BlazorInvoice.Pwa.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddPdfGenerator();
builder.Services.AddBbToast();
builder.Services.AddLocalization();
builder.Services.AddChartJs(options =>
{
    options.ChartJsLocation = "/_content/BlazorInvoice.Weblib/js/chart.umd.js";
    options.ChartJsPluginDatalabelsLocation = "/_content/BlazorInvoice.Weblib/js/chartjs-plugin-datalabels.js";
});

builder.Services.AddSingleton<IInvoiceRepository, InvoiceRepository>();

builder.Services.AddSingleton<IConfigService, ConfigService>();
builder.Services.AddSingleton<IBackupService, BackupService>();
builder.Services.AddSingleton<IUpdateService, FakeUpdateService>();
builder.Services.AddScoped<IStatsRepository, StatsRepository>();
builder.Services.AddScoped<IMauiPathService, FakeMauiPathService>();
builder.Services.AddScoped<IMauiPopupService, FakeMauiPopupService>();

await builder.Build().RunAsync();