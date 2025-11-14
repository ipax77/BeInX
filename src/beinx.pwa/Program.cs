using beinx.db;
using beinx.pwa;
using BlazorInvoice.Pdf;
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

builder.Services.AddLocalization();
builder.Services.AddValidation();

builder.Services.AddChartJs(o =>
{
    o.ChartJsLocation = $"/_content/beinx.web/js/chart.umd.min.js?v=4.5.1";
    o.ChartJsPluginDatalabelsLocation = $"/_content/beinx.web/js/chartjs-plugin-datalabels.js?v=2.2.0";
});

builder.Services.AddBeinxDbServices();
builder.Services.AddBbToast();
builder.Services.AddPdfGenerator();

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
