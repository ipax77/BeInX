using beinx.db.Services;
using beinx.shared;
using beinx.shared.Interfaces;
using pax.XRechnung.NET;
using pax.XRechnung.NET.AnnotatedDtos;
using System.Globalization;
using System.Xml.Linq;

namespace beinx.Tests;

[TestClass]
public sealed class ZugferdValidationTests
{
    [TestMethod]
    public async Task XRechnungLocalValidationRejectsInvalidTaxScheme()
    {
        var invoice = ZugferdTests.GetInvoiceAnnDto();
        invoice.GlobalTaxScheme = "VAT2";
        var xml = new BlazorInvoiceMapper().ToXml(invoice);

        Assert.IsTrue(XmlInvoiceValidator.Validate(xml).IsValid);
        var result = await XmlInvoiceValidator.ValidateSchematron(xml);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Validations.Any(v => v.Code == "BR-CO-04"));
    }

    [TestMethod]
    [DataRow(true, "VAT2")]
    [DataRow(false, "VAT2")]
    [DataRow(true, "VAT")]
    public async Task XRechnungExportUsesLocalValidationAndHonorsSetting(bool validate, string taxScheme)
    {
        var invoice = ZugferdTests.GetInvoiceAnnDto();
        invoice.GlobalTaxScheme = taxScheme;
        var entity = new InvoiceEntity { Id = 1, Info = new InvoiceDtoInfo { InvoiceDto = invoice } };
        var repository = new Repository(entity);
        var config = new Config(new AppConfigDto
        {
            ExportType = ExportType.Xml, ExportValidate = validate, ExportFinalize = true,
            SchematronValidationUri = string.Empty
        });
        var service = new InvoiceService(config, repository, new Pdf(), null!, null!, null!);

        var result = await service.ExportInvoice(1);

        if (validate && taxScheme == "VAT2")
        {
            Assert.Contains("XML Schematron validation failed", result.Error!);
            Assert.Contains("BR-CO-04", result.Error!);
            Assert.IsNull(result.FinalizeResult);
            Assert.IsNull(entity.FinalizeResult);
            Assert.AreEqual(0, repository.Updates);
        }
        else
        {
            Assert.IsNull(result.Error, result.Error);
            Assert.IsNotNull(entity.FinalizeResult);
            Assert.AreEqual(1, repository.Updates);
        }
    }

    [TestMethod]
    [DataRow(19.0)]
    [DataRow(0.0)]
    public void GeneratedInvoicePassesFullValidation(double tax)
    {
        var invoice = ZugferdTests.GetInvoiceAnnDto();
        invoice.GlobalTax = tax;
        var result = ZugferdMapper.ValidateZugferdXml(ZugferdMapper.MapToZugferd(invoice));
        Assert.IsTrue(result.IsValid, result.Error ?? string.Join("\n", result.Validations.Select(m => $"{m.Code}: {m.Message}")));
    }

    [TestMethod]
    [DataRow(0.335)]
    [DataRow(0.345)]
    public void MultipleRoundingSensitiveLinesPassAndTotalsMatch(double price)
    {
        var invoice = ZugferdTests.GetInvoiceAnnDto();
        invoice.InvoiceLines = Enumerable.Range(1, 30).Select(i => new InvoiceLineAnnotationDto
        {
            Id = i.ToString(CultureInfo.InvariantCulture), Name = "Service", QuantityCode = "HUR",
            UnitPrice = price, Quantity = 1.0
        }).ToList();
        var text = ZugferdMapper.MapToZugferd(invoice);
        var result = ZugferdMapper.ValidateZugferdXml(text);
        Assert.IsTrue(result.IsValid, result.Error ?? string.Join("\n", result.Validations.Select(m => $"{m.Code}: {m.Message}")));
        XNamespace ram = "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";
        var xml = XDocument.Parse(text);
        var sum = xml.Descendants(ram + "SpecifiedTradeSettlementLineMonetarySummation")
            .Sum(e => decimal.Parse(e.Element(ram + "LineTotalAmount")!.Value, CultureInfo.InvariantCulture));
        var total = xml.Descendants(ram + "SpecifiedTradeSettlementHeaderMonetarySummation").Single();
        Assert.AreEqual(invoice.InvoiceLines.Sum(line => (decimal)line.LineTotal), sum);
        Assert.AreEqual(sum, decimal.Parse(total.Element(ram + "LineTotalAmount")!.Value, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, false)]
    [DataRow(true, true)]
    public async Task ExportValidatesBeforePdfAndHonorsSetting(bool validate, bool valid)
    {
        var invoice = ZugferdTests.GetInvoiceAnnDto();
        if (!valid) invoice.SellerParty.Name = string.Empty;
        var entity = new InvoiceEntity { Id = 1, Info = new InvoiceDtoInfo { InvoiceDto = invoice } };
        var repository = new Repository(entity);
        var pdf = new Pdf();
        var config = new Config(new AppConfigDto
        {
            ExportType = ExportType.PdfA3, ExportValidate = validate, ExportFinalize = true,
            SchematronValidationUri = "https://unused.invalid"
        });
        var service = new InvoiceService(config, repository, pdf, null!, null!, null!);
        var expectedXml = ZugferdMapper.MapToZugferd(invoice);
        var result = await service.ExportInvoice(1);
        if (validate && !valid)
        {
            Assert.Contains("ZUGFeRD XML validation failed", result.Error!);
            Assert.Contains("BR-06", result.Error!);
            Assert.IsNull(result.FinalizeResult);
            Assert.IsNull(pdf.Xml);
            Assert.AreEqual(0, repository.Updates);
            Assert.IsNull(entity.FinalizeResult);
        }
        else
        {
            Assert.IsNull(result.Error);
            Assert.AreEqual(expectedXml, pdf.Xml);
            Assert.IsNotNull(entity.FinalizeResult);
            Assert.AreEqual(1, repository.Updates);
        }
    }

    private sealed class Pdf : IPdfJsInterop
    {
        public string? Xml { get; private set; }
        public ValueTask<byte[]> CreateInvoicePdfA3Bytes(BlazorInvoiceDto invoice, string cultureName, string hexId, string xmlText)
        {
            Xml = xmlText;
            return ValueTask.FromResult(new byte[] { 1, 2, 3 });
        }
        public ValueTask<string> CreateInvoicePdf(BlazorInvoiceDto invoice, string cultureName) => throw new NotSupportedException();
        public ValueTask<byte[]> CreateInvoicePdfBytes(BlazorInvoiceDto invoice, string cultureName) => throw new NotSupportedException();
        public ValueTask<string> CreateInvoicePdfA3(BlazorInvoiceDto invoice, string cultureName, string hexId, string xmlText) => throw new NotSupportedException();
        public ValueTask<string?> GetXmlString(byte[] pdfBytes) => throw new NotSupportedException();
    }

    private sealed class Config(AppConfigDto config) : IConfigService
    {
        public event Action<AppConfigDto>? OnUpdate { add { } remove { } }
        public Task<AppConfigDto> GetConfig() => Task.FromResult(config);
        public List<CultureInfo> GetSupportedCultures() => [];
        public Task UpdateConfig(AppConfigDto configDto) => throw new NotSupportedException();
        public Task Reload() => throw new NotSupportedException();
        public Task<bool> IsBackupNeeded() => throw new NotSupportedException();
        public Task DownloadBackup() => throw new NotSupportedException();
        public Task RestoreBackup() => throw new NotSupportedException();
    }

    private sealed class Repository(InvoiceEntity entity) : IInvoiceRepository
    {
        public int Updates { get; private set; }
        public Task<InvoiceEntity?> GetByIdAsync(int id) => Task.FromResult<InvoiceEntity?>(entity);
        public Task UpdateAsync(int id, InvoiceDtoInfo info) { Updates++; return Task.CompletedTask; }
        public Task<int> CreateAsync(InvoiceDtoInfo dto, bool isImported = false) => throw new NotSupportedException();
        public Task DeleteAsync(int id) => throw new NotSupportedException();
        public Task<List<InvoiceEntity>> GetAllAsync() => throw new NotSupportedException();
        public Task<List<InvoiceEntity>> FindByYearAsync(int year) => throw new NotSupportedException();
        public Task SetPaidAsync(int id, bool isPaid) => throw new NotSupportedException();
        public Task FinalizeAsync(int id, FinalizeResult result) => throw new NotSupportedException();
        public Task<List<InvoiceListItem>> GetListAsync(int limit = 50, int offset = 0) => throw new NotSupportedException();
        public Task<int> GetCountAsync() => throw new NotSupportedException();
        public Task<List<InvoiceListItem>> GetFilteredListAsync(InvoicesRequest request) => throw new NotSupportedException();
        public Task<int> GetFilteredListCountAsync(InvoicesRequest request) => throw new NotSupportedException();
        public Task<int> GetCountByYearAsync(int year) => throw new NotSupportedException();
        public Task<int> GetCountByPaidStatusAsync(bool isPaid) => throw new NotSupportedException();
        public Task ClearAsync() => throw new NotSupportedException();
    }
}
