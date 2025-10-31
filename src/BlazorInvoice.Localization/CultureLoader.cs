using System.Globalization;
using System.Runtime.Loader;

namespace BlazorInvoice.Localization;

public static class CultureLoader
{
    public static async Task LoadSatelliteAssembliesAsync()
    {
        if (!OperatingSystem.IsBrowser())
        {
            return;
        }
        // Figure out which culture hierarchy we need
        var cultureChain = GetCultureHierarchy(CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);

        // For each culture, try loading its satellite assemblies if present
        foreach (var culture in cultureChain)
        {
            foreach (var asmName in GetSatelliteAssemblyNames())
            {
                var path = $"_framework/{culture}/{asmName}.resources.wasm";
                try
                {
                    // Try to fetch and load the assembly
                    using var stream = await new HttpClient().GetStreamAsync(path);
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    ms.Position = 0;
                    AssemblyLoadContext.Default.LoadFromStream(ms);
                }
                catch
                {
                    // Ignore if culture not found
                }
            }
        }
    }

    private static HashSet<string> GetCultureHierarchy(CultureInfo culture, CultureInfo? uiCulture)
    {
        var list = new HashSet<string>();

        void AddChain(CultureInfo ci)
        {
            while (ci != CultureInfo.InvariantCulture)
            {
                list.Add(ci.Name);
                if (ci == ci.Parent) break;
                ci = ci.Parent;
            }
        }

        AddChain(culture);
        if (uiCulture != null && uiCulture != culture) AddChain(uiCulture);

        return list;
    }

    // Replace with your own assembly names that contain localized resources
    private static readonly string[] ResourceAssemblyNames =
    [
        "BlazorInvoice.Localization"
    ];

    private static string[] GetSatelliteAssemblyNames() => ResourceAssemblyNames;
}
