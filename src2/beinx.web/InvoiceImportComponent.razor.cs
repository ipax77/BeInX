using beinx.shared;
using Microsoft.AspNetCore.Components.Forms;
using pax.XRechnung.NET;
using pax.XRechnung.NET.XmlModels;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace beinx.web;

public partial class InvoiceImportComponent
{
    private string xmlTextInput = string.Empty;

    private string xmlText = string.Empty;
    private string zXmlText = string.Empty;
    private string jsonText = string.Empty;
    private string message = string.Empty;
    private InvoiceValidationResult? schemaResult;
    private XmlInvoice? xmlInvoice;
    private BlazorInvoiceDto? InvoiceDto;
    private InputFile? inputFile;

    private bool isLoading = false;

    private async Task LoadFile(InputFileChangeEventArgs e)
    {
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
            message = Loc["Failed reading json data."];
            return;
        }

        var mapper = new BlazorInvoiceMapper();
        xmlInvoice = mapper.ToXml(dto);
        if (xmlInvoice is null)
        {
            ToastService.ShowError(Loc["Failed mapping json to xml"]);
            message = Loc["Failed mapping json to xml"];
            return;
        }

        await Validate(xmlInvoice);
    }

    private async Task ValidateXml(string? xml)
    {
        if (string.IsNullOrEmpty(xml))
        {
            ToastService.ShowError(Loc["Failed getting xml data"]);
            message = Loc["Failed getting xml data"];
            return;
        }

        var doc = XDocument.Parse(xml);
        var root = doc.Root;

        if (root == null)
        {
            message = Loc["XML has no root element."];
            return;
        }

        var ns = root.Name.NamespaceName;

        if (ns.Contains("CrossIndustryInvoice") || ns.Contains("ferd"))
        {
            await HandleZugferd(xml);
        }
        else if (ns.Contains("xrechnung") || ns.Contains("ubl") || ns.Contains("cii"))
        {
            await HandleXRechnung(xml);
        }
        else
        {
            message = Loc["Unknown xml format."];
        }
    }

    private async Task HandleZugferd(string xml)
    {
        var dto = InvoiceService.GetDtoFromZugferdXmlString(xml);
        if (dto is null)
        {
            message = Loc["Failed mapping to dto"];
            ToastService.ShowError(Loc["Failed mapping to dto"]);
            return;
        }
        var mapper = new BlazorInvoiceMapper();
        var xmlInvoice = mapper.ToXml(dto);
        await Validate(xmlInvoice);
    }

    private async Task HandleXRechnung(string xml)
    {
        var serializer = new XmlSerializer(typeof(XmlInvoice));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var xmlInvoice = (XmlInvoice?)serializer.Deserialize(stream);
        await Validate(xmlInvoice);
    }

    private async Task Validate(XmlInvoice? invoice)
    {
        message = string.Empty;
        zXmlText = string.Empty;
        jsonText = string.Empty;
        xmlInvoice = null;
        InvoiceDto = null;
        schemaResult = null;

        if (invoice is null)
        {
            message = Loc["Failed mapping to final xml"];
            ToastService.ShowError(Loc["Failed mapping to final xml"]);
            return;
        }

        xmlInvoice = invoice;
        schemaResult = XmlInvoiceValidator.Validate(xmlInvoice);
        if (!schemaResult.IsValid)
        {
            message = $"XML {Loc["ValFailed"]}";
            ToastService.ShowError(message);
            return;
        }
        var mapper = new BlazorInvoiceMapper();
        InvoiceDto = mapper.FromXml(invoice);
        xmlText = XmlInvoiceWriter.Serialize(invoice);
        zXmlText = InvoiceService.GetZugferdXmlString(InvoiceDto);
        jsonText = JsonSerializer.Serialize(InvoiceDto, new JsonSerializerOptions() { WriteIndented = true });

        message = Loc["XML parsed and validated successfully."];
        await InvokeAsync(StateHasChanged);
    }

    private async Task Import()
    {
        if (InvoiceDto == null)
        {
            ToastService.ShowError("No invoice to import.");
            return;
        }

        var result = await InvoiceService.ImportInvoice(InvoiceDto);
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