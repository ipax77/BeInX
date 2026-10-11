using beinx.shared;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using pax.XRechnung.NET;
using pax.XRechnung.NET.XmlModels;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace beinx.web;

public partial class InvoiceImportComponent
{
    private class InvoiceImportResult
    {
        public string XmlText { get; set; } = string.Empty;
        public string ZXmlText { get; set; } = string.Empty;
        public string JsonText { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public InvoiceValidationResult? SchemaResult { get; set; }
        public XmlInvoice? XmlInvoice { get; set; }
        public BlazorInvoiceDto? InvoiceDto { get; set; }
    }

    private string xmlTextInput = string.Empty;
    private InvoiceImportResult? importResult;
    private InputFile? inputFile;

    private bool isLoading = false;

    private void Reset()
    {
        xmlTextInput = string.Empty;
        importResult = null;
        isLoading = false;
    }

    private async Task LoadFile(InputFileChangeEventArgs e)
    {
        importResult = null;
        isLoading = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            var file = e.File;
            if (file is null)
            {
                ToastService.ShowError(Loc["No file selected."]);
                return;
            }

            // Determine file type
            var fileName = file.Name.ToLowerInvariant();
            using var stream = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024).CopyToAsync(stream);
            stream.Position = 0;

            if (fileName.EndsWith(".json"))
            {
                stream.Position = 0;
                var dto = await JsonSerializer.DeserializeAsync<BlazorInvoiceDto>(stream);
                await ValidateJson(dto);
            }
            else if (fileName.EndsWith(".xml"))
            {
                var xml = Encoding.UTF8.GetString(stream.ToArray());
                await ValidateXml(xml);
            }
            else if (fileName.EndsWith(".pdf"))
            {
                var pdfBytes = stream.ToArray();
                var extractedXml = await PdfJsInterop.GetXmlString(pdfBytes);
                await ValidateXml(extractedXml);
            }
            else
            {
                ToastService.ShowError("Unsupported file type. Please choose .xml or .pdf.");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError("Failed to load file: " + ex.Message);
        }
        finally
        {
            isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ValidateText()
    {
        importResult = null;
        if (string.IsNullOrEmpty(xmlTextInput))
        {
            return;
        }
        isLoading = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            if (xmlTextInput.StartsWith('{'))
            {
                var dto = JsonSerializer.Deserialize<BlazorInvoiceDto>(xmlTextInput);
                await ValidateJson(dto);
            }
            else
            {
                await ValidateXml(xmlTextInput);
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError("Failed to load file: " + ex.Message);
        }
        finally
        {
            isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ValidateJson(BlazorInvoiceDto? dto)
    {
        if (dto is null)
        {
            importResult = new InvoiceImportResult { Message = Loc["Failed reading json data."] };
            return;
        }

        var mapper = new BlazorInvoiceMapper();
        var xmlInvoice = mapper.ToXml(dto);
        if (xmlInvoice is null)
        {
            ToastService.ShowError(Loc["Failed mapping json to xml"]);
            importResult = new InvoiceImportResult { Message = Loc["Failed mapping json to xml"] };
            return;
        }

        var result = await XmlInvoiceValidator.ValidateSchematron(xmlInvoice);
        if (SetValidationResult(result))
            CompleteValidation(xmlInvoice);
    }

    private async Task ValidateXml(string? xml)
    {
        importResult = null;
        if (string.IsNullOrEmpty(xml))
        {
            ToastService.ShowError(Loc["Failed getting xml data"]);
            importResult = new InvoiceImportResult { Message = Loc["Failed getting xml data"] };
            return;
        }
        Logger.LogInformation(xml);
        var doc = XDocument.Parse(xml);
        var root = doc.Root;

        if (root == null)
        {
            importResult = new InvoiceImportResult { Message = Loc["XML has no root element."] };
            return;
        }

        var ns = root.Name.NamespaceName;

        if (ns.Contains("CrossIndustryInvoice") || ns.Contains("ferd"))
        {
            HandleZugferd(xml);
        }
        else if (ns.Contains("xrechnung") || ns.Contains("ubl") || ns.Contains("cii"))
        {
            await HandleXRechnung(xml);
        }
        else
        {
            importResult = new InvoiceImportResult { Message = Loc["Unknown xml format."] };
        }
    }

    private void HandleZugferd(string xml)
    {
        if (!SetValidationResult(ZugferdXmlValidator.Validate(xml)))
            return;

        var dto = InvoiceService.GetDtoFromZugferdXmlString(xml);
        if (dto is null)
        {
            importResult = new InvoiceImportResult { Message = Loc["Failed mapping to dto"] };
            ToastService.ShowError(Loc["Failed mapping to dto"]);
            return;
        }
        var mapper = new BlazorInvoiceMapper();
        var xmlInvoice = mapper.ToXml(dto);
        CompleteValidation(xmlInvoice);
    }

    private async Task HandleXRechnung(string xml)
    {
        if (!SetValidationResult(await XmlInvoiceValidator.ValidateSchematron(xml)))
            return;

        var serializer = new XmlSerializer(typeof(XmlInvoice));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var xmlInvoice = (XmlInvoice?)serializer.Deserialize(stream);
        CompleteValidation(xmlInvoice);
    }

    private bool SetValidationResult(InvoiceValidationResult result)
    {
        importResult = new InvoiceImportResult { SchemaResult = result };
        if (!result.IsValid)
        {
            importResult.Message = $"XML {Loc["ValFailed"]}";
            ToastService.ShowError(importResult.Message);
            return false;
        }
        return true;
    }

    private void CompleteValidation(XmlInvoice? invoice)
    {
        if (invoice is null)
        {
            importResult = new InvoiceImportResult { Message = Loc["Failed mapping to final xml"] };
            ToastService.ShowError(Loc["Failed mapping to final xml"]);
            return;
        }

        importResult!.XmlInvoice = invoice;
        var mapper = new BlazorInvoiceMapper();
        importResult.InvoiceDto = mapper.FromXml(invoice);
        importResult.XmlText = XmlInvoiceWriter.Serialize(invoice);
        if (importResult.InvoiceDto != null)
        {
            importResult.ZXmlText = InvoiceService.GetZugferdXmlString(importResult.InvoiceDto);
            importResult.JsonText = JsonSerializer.Serialize(importResult.InvoiceDto, new JsonSerializerOptions() { WriteIndented = true });
        }

        importResult.Message = Loc["XML parsed and validated successfully."];
    }

    private async Task Import()
    {
        if (importResult?.InvoiceDto == null || importResult.SchemaResult?.IsValid != true)
        {
            ToastService.ShowError("No invoice to import.");
            return;
        }

        var result = await InvoiceService.ImportInvoice(importResult.InvoiceDto);
        if (!string.IsNullOrEmpty(result.Error))
        {
            ToastService.ShowError(result.Error);
        }
        else
        {
            ToastService.ShowSuccess(Loc["Invoice saved successfully."]);
        }
    }
}
