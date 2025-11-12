using beinx.shared;
using beinx.shared.Interfaces;
using pax.XRechnung.NET;
using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.XmlModels;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace beinx.db.Services;

public class InvoiceService(IConfigService configService, IInvoiceRepository invoiceRepository, IPdfJsInterop pdfJsInterop) : IInvoiceService
{
    public async Task<ExportResult> ExportInvoice(int invoiceId)
    {
        try
        {
            var config = await configService.GetConfig();

            if (config.ExportType == ExportType.PdfA3)
            {
                await AddReplaceOrDeletePdf(null, invoiceId);
            }

            var invoiceInfo = await invoiceRepository.GetByIdAsync(invoiceId);
            ArgumentNullException.ThrowIfNull(invoiceInfo, nameof(invoiceInfo));
            var invoiceDto = invoiceInfo.Info.InvoiceDto;
            var mapper = new BlazorInvoiceMapper();
            var xmlInvoice = mapper.ToXml(invoiceDto);
            ArgumentNullException.ThrowIfNull(xmlInvoice, nameof(xmlInvoice));

            var xmlText = GetXmlText(xmlInvoice, invoiceDto, config.ExportType);
            var pdfBytes = await GetPdfBytes(invoiceDto, config.ExportType, config.CultureName, xmlText);

            if (config.ExportEmbedPdf && config.ExportType != ExportType.PdfA3)
            {
                var docRef = await AddReplaceOrDeletePdf(Convert.ToBase64String(pdfBytes), invoiceId);
                invoiceDto.EmbedPdf(docRef);
                xmlInvoice = mapper.ToXml(invoiceDto);
            }

            if (config.ExportValidate)
            {
                var schemaResult = XmlInvoiceValidator.Validate(xmlInvoice);
                if (!schemaResult.IsValid)
                {
                    return new(new(DateTime.UtcNow, string.Empty, Array.Empty<byte>()), string.Empty, Error: "XML Schema validation failed.");
                }
                if (!string.IsNullOrEmpty(config.SchematronValidationUri))
                {
                    var schematronResult = await XmlInvoiceValidator.ValidateSchematron(xmlInvoice, new Uri(config.SchematronValidationUri));
                    if (!schematronResult.IsValid)
                    {
                        return new(new(DateTime.UtcNow, string.Empty, Array.Empty<byte>()), string.Empty, Error: "XML Schematron validation failed.");
                    }
                }
            }
            FinalizeResult finalizeResult;
            if (config.ExportFinalize)
            {
                finalizeResult = await FinalizeInvoice(invoiceId, xmlInvoice);
            }
            else
            {
                var xmlBytes = System.Text.Encoding.UTF8.GetBytes(xmlText);
                var hash = SHA1.HashData(xmlBytes);
                finalizeResult = new(DateTime.UtcNow, Convert.ToBase64String(hash), xmlBytes);
            }

            var fileNameWithoutExtension = $"{invoiceDto.SellerParty.Name}_{invoiceDto.Id}";
            fileNameWithoutExtension = SanitizeFileName(fileNameWithoutExtension);

            var fileName = config.ExportType switch
            {
                ExportType.Xml => fileNameWithoutExtension + ".xml",
                ExportType.Pdf => fileNameWithoutExtension + ".pdf",
                ExportType.XmlAndPdf => fileNameWithoutExtension + ".zip",
                ExportType.PdfA3 => fileNameWithoutExtension + ".pdf",
                _ => throw new NotSupportedException($"Export type {config.ExportType} is not supported.")
            };

            if (config.ExportType == ExportType.Pdf)
            {
                return new(finalizeResult with { Blob = pdfBytes, MimeType = "application/pdf" }, fileName, null);
            }
            else if (config.ExportType == ExportType.Xml)
            {
                return new(finalizeResult with { MimeType = "application/xml" }, fileName, null);
            }
            else if (config.ExportType == ExportType.PdfA3)
            {
                return new(finalizeResult with { Blob = pdfBytes, MimeType = "application/pdf" }, fileName, null);
            }
            else
            {
                var zipBytes = CreateZipFile(finalizeResult.Blob, pdfBytes, fileNameWithoutExtension);
                return new(finalizeResult with { Blob = zipBytes, MimeType = "application/zip" }, fileName, null);
            }
        }
        catch (Exception ex)
        {
            return new ExportResult(null, string.Empty, $"Error during export: {ex.Message}");
        }
    }

    private static string GetXmlText(XmlInvoice xmlInvoice, BlazorInvoiceDto invoiceDto, ExportType exportType)
    {
        return exportType switch
        {
            ExportType.Xml => XmlInvoiceWriter.Serialize(xmlInvoice),
            ExportType.Pdf => string.Empty,
            ExportType.XmlAndPdf => XmlInvoiceWriter.Serialize(xmlInvoice),
            ExportType.PdfA3 => ZugferdMapper.MapToZugferd(invoiceDto),
            _ => throw new NotSupportedException($"Export type {exportType} is not supported.")
        };
    }

    private async Task<byte[]> GetPdfBytes(BlazorInvoiceDto invoiceDto, ExportType exportType, string cultureName, string xmlText)
    {
        return exportType switch
        {
            ExportType.Xml => Array.Empty<byte>(),
            ExportType.Pdf => await pdfJsInterop.CreateInvoicePdfBytes(invoiceDto, cultureName),
            ExportType.XmlAndPdf => await pdfJsInterop.CreateInvoicePdfBytes(invoiceDto, cultureName),
            ExportType.PdfA3 => await GetPdfA3Bytes(invoiceDto, cultureName, xmlText),
            _ => throw new NotSupportedException($"Export type {exportType} is not supported.")
        };
    }

    private async Task<byte[]> GetPdfA3Bytes(BlazorInvoiceDto invoiceDto, string cultureName, string xmlText)
    {
        var hexId = GenerateHexDocumentId();
        return await pdfJsInterop.CreateInvoicePdfA3Bytes(invoiceDto, cultureName, hexId, xmlText);
    }

    private static string GenerateHexDocumentId()
    {
        byte[] bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLower();
    }

    private static byte[] CreateZipFile(byte[] xmlBytes, byte[] pdfBytes, string fileNameWithoutExtension)
    {
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            var file1Entry = archive.CreateEntry(fileNameWithoutExtension + ".xml");
            using (var entryStream = file1Entry.Open())
            {
                entryStream.Write(xmlBytes, 0, xmlBytes.Length);
            }

            var file2Entry = archive.CreateEntry(fileNameWithoutExtension + ".pdf");
            using (var entryStream = file2Entry.Open())
            {
                entryStream.Write(pdfBytes, 0, pdfBytes.Length);
            }
        }
        zipStream.Position = 0;
        return zipStream.ToArray();
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleaned = new string(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return cleaned.Replace(" ", "_").Trim();
    }

    public async Task<DocumentReferenceAnnotationDto?> AddReplaceOrDeletePdf(string? base64String, int invoiceId)
    {
        var invoice = await invoiceRepository.GetByIdAsync(invoiceId);
        if (invoice == null) return null;

        var pdf = invoice.Info.InvoiceDto.AdditionalDocumentReferences.FirstOrDefault(f => f.MimeCode == "application/pdf");

        if (string.IsNullOrEmpty(base64String))
        {
            if (pdf is not null)
            {
                invoice.Info.InvoiceDto.AdditionalDocumentReferences.Remove(pdf);
                await invoiceRepository.UpdateAsync(invoiceId, invoice.Info);
            }
            return null;
        }

        if (pdf is null)
        {
            pdf = new DocumentReferenceAnnotationDto
            {
                Id = Guid.NewGuid().ToString(),
                MimeCode = "application/pdf",
                DocumentDescription = "PDF",
                FileName = "Invoice.pdf",
                Content = base64String
            };
            invoice.Info.InvoiceDto.AdditionalDocumentReferences.Add(pdf);
        }
        else
        {
            pdf.Content = base64String;
        }
        await invoiceRepository.UpdateAsync(invoiceId, invoice.Info);
        return pdf;
    }

    public async Task<FinalizeResult> FinalizeInvoice(int invoiceId, XmlInvoice xmlInvoice)
    {
        var invoice = await invoiceRepository.GetByIdAsync(invoiceId);
        ArgumentNullException.ThrowIfNull(invoice, nameof(invoice));

        var xmlText = XmlInvoiceWriter.Serialize(xmlInvoice);
        ArgumentNullException.ThrowIfNull(invoice, nameof(xmlText));

        var bytes = Encoding.UTF8.GetBytes(xmlText);
        var hash = SHA1.HashData(bytes);

        invoice.FinalizeResult = new FinalizeResult(DateTime.UtcNow, Convert.ToBase64String(hash), bytes);

        await invoiceRepository.UpdateAsync(invoiceId, invoice.Info);
        return invoice.FinalizeResult;
    }
}
