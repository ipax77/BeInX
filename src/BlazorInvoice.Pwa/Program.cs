using BlazorInvoice.IndexedDb.Services;
using BlazorInvoice.Pdf;
using BlazorInvoice.Pwa;
using BlazorInvoice.Pwa.Services;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using pax.BBToast;
using pax.BlazorChartJs;
using System.Globalization;

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

builder.Services.AddSingleton<IIndexedDbService, IndexedDbService>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();

builder.Services.AddSingleton<IConfigService, ConfigService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddSingleton<IUpdateService, FakeUpdateService>();
builder.Services.AddScoped<IStatsRepository, StatsService>();
builder.Services.AddScoped<IMauiPathService, FakeMauiPathService>();
builder.Services.AddScoped<IMauiPopupService, FakeMauiPopupService>();

var host = builder.Build();

const string defaultCulture = "en-US";

var js = host.Services.GetRequiredService<IJSRuntime>();
var result = await js.InvokeAsync<string>("blazorCulture.get");
var culture = CultureInfo.GetCultureInfo(result ?? defaultCulture);

if (result == null)
{
    await js.InvokeVoidAsync("blazorCulture.set", defaultCulture);
}

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();