using System.Reflection;
using System.Xml.Linq;
using beinx.db.Services;
using beinx.loc;
using beinx.shared;
using beinx.web;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using pax.BBToast;
using pax.XRechnung.NET;

namespace beinx.Tests;

[TestClass]
public sealed class InvoiceImportValidationTests
{
    [TestMethod]
    [DataRow("VAT")]
    [DataRow("VAT2")]
    public async Task ImportAndEditReportSameXRechnungFindings(string scheme)
    {
        var component = CreateComponent();
        var invoice = ZugferdTests.GetInvoiceAnnDto();
        invoice.GlobalTaxScheme = scheme;
        var xmlInvoice = new BlazorInvoiceMapper().ToXml(invoice);
        var editResult = await XmlInvoiceValidator.ValidateSchematron(xmlInvoice);

        await Invoke(component, "ValidateXml", XmlInvoiceWriter.Serialize(xmlInvoice));
        var importResult = Validation(component);

        CollectionAssert.AreEquivalent(
            editResult.Validations.Select(v => $"{v.Code}: {v.Severity}: {v.Message}").ToArray(),
            importResult.Validations.Select(v => $"{v.Code}: {v.Severity}: {v.Message}").ToArray());
        Assert.IsTrue(importResult.Validations.Any(v => v.Code == "UBL-CR-520"));
        Assert.IsTrue(importResult.Validations.Any(v => v.Code == "UBL-CR-521"));
        if (scheme == "VAT2")
        {
            CollectionAssert.AreEquivalent(
                new[] { "BR-S-02", "BR-CO-26", "BR-47", "BR-48", "BR-CO-17", "BR-CO-04" },
                importResult.Validations.Where(v => v.Severity == System.Xml.Schema.XmlSeverityType.Error)
                    .Select(v => v.Code).ToArray());
        }
        else
            Assert.IsTrue(importResult.IsValid);
    }

    [TestMethod]
    [DataRow(false, true)]
    [DataRow(false, false)]
    [DataRow(true, true)]
    [DataRow(true, false)]
    public async Task XmlImportValidatesOriginalDocument(bool zugferd, bool valid)
    {
        var component = CreateComponent();
        var xml = CreateXml(zugferd);
        if (!valid)
        {
            foreach (var code in xml.Descendants().Where(e =>
                zugferd ? e.Name.LocalName == "TypeCode" && e.Value == "VAT"
                : e.Name.LocalName == "TaxScheme").ToList())
            {
                if (zugferd) code.Value = "VAT2";
                else code.Elements().Single(e => e.Name.LocalName == "ID").Value = "VAT2";
            }
        }

        await Invoke(component, "ValidateXml", xml.ToString());

        var result = Validation(component);
        Assert.AreEqual(valid, result.IsValid, result.Error ?? string.Join("\n", result.Validations.Select(v => v.Message)));
        Assert.Contains(zugferd ? "ZUGFeRD" : "XRechnung", result.Conformity!);
        if (valid)
            Assert.IsNotNull(ResultProperty(component, "InvoiceDto"));
        else
        {
            Assert.IsTrue(result.Validations.Any(v => v.Severity == System.Xml.Schema.XmlSeverityType.Error));
            Assert.IsNull(ResultProperty(component, "InvoiceDto"));
            await AssertImportBlocked(component);
        }
    }

    [TestMethod]
    public async Task ZugferdImportRejectsTotalsThatConversionWouldRecalculate()
    {
        var component = CreateComponent();
        var xml = CreateXml(true);
        xml.Descendants().Single(e => e.Name.LocalName == "GrandTotalAmount").Value = "999.00";

        await Invoke(component, "ValidateXml", xml.ToString());

        Assert.IsFalse(Validation(component).IsValid);
        Assert.IsTrue(Validation(component).Validations.Any(v => v.Message.Contains("[BR-CO-16]", StringComparison.Ordinal)),
            string.Join("\n", Validation(component).Validations.Select(v => $"{v.Code}: {v.Message}")));
        Assert.IsNull(ResultProperty(component, "InvoiceDto"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SchemaErrorsAreReportedBeforeMapping(bool zugferd)
    {
        var component = CreateComponent();
        var xml = CreateXml(zugferd);
        xml.Root!.Add(new XElement(xml.Root.Name.Namespace + "UnexpectedElement"));

        await Invoke(component, "ValidateXml", xml.ToString());

        Assert.IsFalse(Validation(component).IsValid);
        Assert.IsTrue(Validation(component).Validations.Any(v => v.Severity == System.Xml.Schema.XmlSeverityType.Error));
        Assert.IsNull(ResultProperty(component, "InvoiceDto"));
    }

    [TestMethod]
    public async Task InvalidInputClearsPreviouslyValidatedInvoice()
    {
        var component = CreateComponent();
        await Invoke(component, "ValidateXml", CreateXml(false).ToString());
        Assert.IsNotNull(ResultProperty(component, "InvoiceDto"));

        await Invoke(component, "ValidateXml", "<unknown />");

        Assert.IsNull(ResultProperty(component, "InvoiceDto"));
        await AssertImportBlocked(component);
    }

    [TestMethod]
    [DataRow("VAT", true)]
    [DataRow("VAT2", false)]
    public async Task JsonImportUsesLocalXRechnungValidation(string scheme, bool valid)
    {
        var component = CreateComponent();
        var invoice = ZugferdTests.GetInvoiceAnnDto();
        invoice.GlobalTaxScheme = scheme;

        await Invoke(component, "ValidateJson", invoice);

        Assert.AreEqual(valid, Validation(component).IsValid);
        Assert.AreEqual(valid, ResultProperty(component, "InvoiceDto") != null);
    }

    private static XDocument CreateXml(bool zugferd)
    {
        var invoice = ZugferdTests.GetInvoiceAnnDto();
        return XDocument.Parse(zugferd ? ZugferdMapper.MapToZugferd(invoice)
            : XmlInvoiceWriter.Serialize(new BlazorInvoiceMapper().ToXml(invoice)));
    }

    private static InvoiceImportComponent CreateComponent()
    {
        var component = new InvoiceImportComponent();
        // Mapping methods need no repositories; attempting to save would fail.
        SetProperty(component, "InvoiceService", new InvoiceService(null!, null!, null!, null!, null!, null!));
        SetProperty(component, "Loc", new Localizer());
        SetProperty(component, "ToastService", new ToastService());
        SetProperty(component, "Logger", NullLogger<InvoiceImportComponent>.Instance);
        return component;
    }

    private static void SetProperty(object component, string name, object value) =>
        component.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(component, value);

    private static async Task Invoke(object component, string name, params object[] args) =>
        await (Task)component.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(component, args)!;

    private static object? ResultProperty(object component, string name)
    {
        var result = component.GetType().GetField("importResult", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component);
        return result?.GetType().GetProperty(name)!.GetValue(result);
    }

    private static InvoiceValidationResult Validation(object component) =>
        (InvoiceValidationResult)ResultProperty(component, "SchemaResult")!;

    private static async Task AssertImportBlocked(InvoiceImportComponent component)
    {
        var toast = (IToastService)component.GetType().GetProperty("ToastService", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component)!;
        string? message = null;
        toast.OnShow += options => message = options.Message;
        await Invoke(component, "Import");
        Assert.AreEqual("No invoice to import.", message);
    }

    private sealed class Localizer : IStringLocalizer<InvoiceLoc>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
