using BlazorInvoice.Shared;
using pax.XRechnung.NET;
using System.IO.Compression;
using System.Security.Cryptography;

namespace BlazorInvoice.Db.Repository;

public partial class InvoiceRepository
{
    public async Task<ExportResult> ExportInvoice(int invoiceId, byte[] pdfBytes)
    {
        var config = await configService.GetConfig();

        if (config.ExportEmbedPdf)
        {
            await AddReplaceOrDeletePdf(Convert.ToBase64String(pdfBytes), invoiceId);
        }
        else
        {
            await AddReplaceOrDeletePdf(null, invoiceId);
        }

        var _invoiceInfo = await GetInvoice(invoiceId);
        if (_invoiceInfo is null)
        {
            return new(new(DateTime.UtcNow, string.Empty, []), string.Empty, Error: "Invoice not found");
        }
        var mapper = new BlazorInvoiceMapper();
        var xmlInvoice = mapper.ToXml(_invoiceInfo.InvoiceDto);
        ArgumentNullException.ThrowIfNull(xmlInvoice, nameof(xmlInvoice));

        if (config.ExportValidate)
        {
            var schemaResult = XmlInvoiceValidator.Validate(xmlInvoice);
            if (!schemaResult.IsValid)
            {
                return new(new(DateTime.UtcNow, string.Empty, []), string.Empty, Error: "XML Schema validation failed.");
            }
            if (!string.IsNullOrEmpty(config.SchematronValidationUri))
            {
                var schematronResult = await XmlInvoiceValidator
                    .ValidateSchematron(xmlInvoice, new Uri(config.SchematronValidationUri));
                if (!schematronResult.IsValid)
                {
                    return new(new(DateTime.UtcNow, string.Empty, []), string.Empty, Error: "XML Schematron validation failed.");
                }
            }
        }
        FinalizeResult finalizeResult;
        if (config.ExportFinalize)
        {
            finalizeResult = await FinalizeInvoice(_invoiceInfo.InvoiceId, xmlInvoice);
        }
        else
        {
            string xmlText = XmlInvoiceWriter.Serialize(xmlInvoice);
            var xmlBytes = System.Text.Encoding.UTF8.GetBytes(xmlText);
            var hash = SHA1.HashData(xmlBytes);
            finalizeResult = new(DateTime.UtcNow, Convert.ToBase64String(hash), xmlBytes);
        }

        var fileNameWithoutExtension = $"{_invoiceInfo.InvoiceDto.SellerParty.Name}_{_invoiceInfo.InvoiceDto.Id}";
        fileNameWithoutExtension = SanitizeFileName(fileNameWithoutExtension);

        if (config.ExportType == ExportType.Pdf)
        {
            var fileName = fileNameWithoutExtension + ".pdf";
            return new(finalizeResult with { Blob = pdfBytes, MimeType = "application/pdf" }, fileName, null);
        }
        else if (config.ExportType == ExportType.Xml)
        {
            var fileName = fileNameWithoutExtension + ".xml";
            return new(finalizeResult with { MimeType = "application/xml" }, fileName, null);
        }
        else
        {
            // return zip file with pdf and xml
            var fileName = fileNameWithoutExtension + ".zip";
            var zipBytes = CreateZipFile(finalizeResult.Blob, pdfBytes, fileNameWithoutExtension);
            return new(finalizeResult with { Blob = zipBytes, MimeType = "application/zip" }, fileName, null);
        }
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
        var cleaned = new string(fileName
            .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
            .ToArray());

        // Optional: Leerzeichen durch Unterstrich ersetzen, Unicode normalisieren etc.
        return cleaned.Replace(" ", "_").Trim();
    }
}


