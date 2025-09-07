using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.XmlModels;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using pax.XRechnung.NET;
using pax.XRechnung.NET.BaseDtos;
using BlazorInvoice.Shared.ZUGFeRD;
using System.IO.Compression;

namespace BlazorInvoice.IndexedDb.Services
{
    public partial class InvoiceRepository : IInvoiceRepository
    {
        private readonly IIndexedDbService _indexedDbService;
        private readonly IConfigService _configService;
        private readonly IPdfJsInterop _pdfJsInterop;
        private readonly ILogger<InvoiceRepository> _logger;

        public InvoiceRepository(IIndexedDbService indexedDbService, IConfigService configService, IPdfJsInterop pdfJsInterop, ILogger<InvoiceRepository> logger)
        {
            _indexedDbService = indexedDbService;
            _configService = configService;
            _pdfJsInterop = pdfJsInterop;
            _logger = logger;
        }

        // TempInvoice
        public async Task DeleteTempInvoice()
        {
            await _indexedDbService.DeleteTempInvoice();
        }

        public async Task<InvoiceDtoInfo?> GetTempInvoice()
        {
            return await _indexedDbService.GetTempInvoice();
        }

        public async Task<bool> HasTempInvoice()
        {
            return await _indexedDbService.HasTempInvoice();
        }

        public async Task SaveTempInvoice(InvoiceDtoInfo request)
        {
            await _indexedDbService.SaveTempInvoice(request);
        }
        // End TempInvoice

        public async Task<ExportResult> ExportInvoice(int invoiceId)
        {
            try
            {
                var config = await _configService.GetConfig();

                if (config.ExportType == ExportType.PdfA3)
                {
                    await AddReplaceOrDeletePdf(null, invoiceId);
                }

                var invoiceInfo = await GetInvoice(invoiceId);
                ArgumentNullException.ThrowIfNull(invoiceInfo, nameof(invoiceInfo));

                var mapper = new BlazorInvoiceMapper();
                var xmlInvoice = mapper.ToXml(invoiceInfo.InvoiceDto);
                ArgumentNullException.ThrowIfNull(xmlInvoice, nameof(xmlInvoice));

                var xmlText = GetXmlText(xmlInvoice, invoiceInfo.InvoiceDto, config.ExportType);
                var pdfBytes = await GetPdfBytes(invoiceInfo.InvoiceDto, config.ExportType, config.CultureName, xmlText);

                if (config.ExportEmbedPdf && config.ExportType != ExportType.PdfA3)
                {
                    var docRef = await AddReplaceOrDeletePdf(Convert.ToBase64String(pdfBytes), invoiceId);
                    invoiceInfo.InvoiceDto.EmbedPdf(docRef);
                    xmlInvoice = mapper.ToXml(invoiceInfo.InvoiceDto);
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

                var fileNameWithoutExtension = $"{invoiceInfo.InvoiceDto.SellerParty.Name}_{invoiceInfo.InvoiceDto.Id}";
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
                ExportType.Pdf => await _pdfJsInterop.CreateInvoicePdfBytes(invoiceDto, cultureName),
                ExportType.XmlAndPdf => await _pdfJsInterop.CreateInvoicePdfBytes(invoiceDto, cultureName),
                ExportType.PdfA3 => await GetPdfA3Bytes(invoiceDto, cultureName, xmlText),
                _ => throw new NotSupportedException($"Export type {exportType} is not supported.")
            };
        }

        private async Task<byte[]> GetPdfA3Bytes(BlazorInvoiceDto invoiceDto, string cultureName, string xmlText)
        {
            var hexId = GenerateHexDocumentId();
            return await _pdfJsInterop.CreateInvoicePdfA3Bytes(invoiceDto, cultureName, hexId, xmlText);
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
            var invoice = await _indexedDbService.GetInvoice(invoiceId);
            if (invoice == null) return null;

            var pdf = invoice.Info.InvoiceDto.AdditionalDocumentReferences.FirstOrDefault(f => f.MimeCode == "application/pdf");

            if (string.IsNullOrEmpty(base64String))
            {
                if (pdf is not null)
                {
                    invoice.Info.InvoiceDto.AdditionalDocumentReferences.Remove(pdf);
                    await _indexedDbService.UpdateInvoice(invoice);
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
            await _indexedDbService.UpdateInvoice(invoice);
            return pdf;
        }

        public async Task<DocumentReferenceAnnotationDto?> AddReplaceOrDeleteSellerLogo(int invoiceId, CancellationToken token)
        {
            var invoice = await _indexedDbService.GetInvoice(invoiceId);
            if (invoice is null || invoice.Info.SellerId is null)
            {
                return null;
            }

            var seller = await _indexedDbService.GetParty(invoice.Info.SellerId.Value);
            if (seller is null)
            {
                return null;
            }

            var desc = "Seller Logo";
            var logoId = seller.Logo?.Id ?? string.Empty;
            var base64String = seller.Logo?.Content;
            var docRef = invoice.Info.InvoiceDto.AdditionalDocumentReferences.FirstOrDefault(f => f.DocumentDescription == desc);

            if (string.IsNullOrEmpty(base64String))
            {
                if (docRef is not null)
                {
                    invoice.Info.InvoiceDto.AdditionalDocumentReferences.Remove(docRef);
                    await _indexedDbService.UpdateInvoice(invoice);
                }
                return null;
            }
            else
            {
                if (docRef is null)
                {
                    docRef = new DocumentReferenceAnnotationDto
                    {
                        Id = logoId,
                        MimeCode = "image/png",
                        DocumentDescription = desc,
                        FileName = "SellerLogo.png",
                        Content = base64String
                    };
                    invoice.Info.InvoiceDto.AdditionalDocumentReferences.Add(docRef);
                }
                else
                {
                    docRef.Content = base64String;
                }
                await _indexedDbService.UpdateInvoice(invoice);
            }
            return docRef;
        }

        public async Task<int> CreateInvoiceCopy(int invoiceId)
        {
            var invoice = await _indexedDbService.GetInvoice(invoiceId);
            if (invoice == null) return 0;

            var invoiceDto = invoice.Info.InvoiceDto;
            invoiceDto.Id = invoiceDto.Id + "_copy";
            invoiceDto.IssueDate = DateTime.UtcNow;
            invoiceDto.DueDate = DateTime.UtcNow.AddDays(14);
            invoiceDto.InvoiceLines.Clear();
            invoiceDto.AdditionalDocumentReferences.Clear();

            return await CreateInvoice(invoiceDto, invoice.Info.SellerId ?? 0, invoice.Info.BuyerId ?? 0, invoice.Info.PaymentId ?? 0);
        }

        public async Task<FinalizeResult> FinalizeInvoice(int invoiceId, XmlInvoice xmlInvoice, CancellationToken token = default)
        {
            var invoice = await _indexedDbService.GetInvoice(invoiceId);
            ArgumentNullException.ThrowIfNull(invoice, nameof(invoice));

            var xmlText = XmlInvoiceWriter.Serialize(xmlInvoice);
            ArgumentNullException.ThrowIfNull(invoice, nameof(xmlText));

            var bytes = Encoding.UTF8.GetBytes(xmlText);
            var hash = SHA1.HashData(bytes);

            invoice.FinalizeResult = new FinalizeResult(DateTime.UtcNow, Convert.ToBase64String(hash), bytes);

            await _indexedDbService.UpdateInvoice(invoice);
            return invoice.FinalizeResult;
        }

        public async Task<XmlInvoice?> GetXmlInvoice(int invoiceId, CancellationToken token = default)
        {
            var invoice = await _indexedDbService.GetInvoice(invoiceId);
            if (invoice?.FinalizeResult == null) return null;

            var serializer = new XmlSerializer(typeof(XmlInvoice));
            using var stream = new MemoryStream(invoice.FinalizeResult.Blob);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return (XmlInvoice?)serializer.Deserialize(reader);
        }

        public async Task<int> ImportInvoice(XmlInvoice invoice, CancellationToken token = default)
        {
            var validationResult = XmlInvoiceValidator.Validate(invoice);
            if (!validationResult.IsValid)
            {
                return 0;
            }
            var mapper = new BlazorInvoiceMapper();
            var dto = mapper.FromXml(invoice);

            var sellerId = await IdentifyOrCreateParty(dto.SellerParty, true, token);
            var buyerId = await IdentifyOrCreateParty(dto.BuyerParty, false, token);
            var paymentId = await IdentifyOrCreatePayment(dto.PaymentMeans, token);
            var invoiceId = await CreateInvoice(dto, sellerId, buyerId, paymentId, true, token);
            await FinalizeInvoice(invoiceId, invoice, token);
            return invoiceId;
        }

        private async Task<int> IdentifyOrCreateParty(IPartyBaseDto party, bool isSeller, CancellationToken token)
        {
            var parties = await _indexedDbService.GetAllParties();
            var existing = parties.FirstOrDefault(p => p.IsSeller == isSeller && p.Party.Name == party.Name && p.Party.TaxId == party.TaxId);
            if (existing != null) return existing.Id;
            return await _indexedDbService.CreateParty(party, isSeller);
        }

        private async Task<int> IdentifyOrCreatePayment(IPaymentMeansBaseDto paymentMeans, CancellationToken token)
        {
            var payments = await _indexedDbService.GetAllPaymentMeans();
            var existing = payments.FirstOrDefault(p => p.Payment.Iban == paymentMeans.Iban);
            if (existing != null) return existing.Id;
            return await _indexedDbService.CreatePaymentMeans(paymentMeans);
        }

        public async Task<int> SeedTestInvoice()
        {
            var invoiceDto = GetInvoiceAnnDto();
            var mapper = new BlazorInvoiceMapper();
            var xmlInvoice = mapper.ToXml(invoiceDto);
            return await ImportInvoice(xmlInvoice);
        }

        public async Task SeedTestInvoices(int count)
        {
            DateTime currentDate = new(DateTime.Today.Year, 1, 1);
            var dayStep = 365 / count;
            var mapper = new BlazorInvoiceMapper();
            for (int i = 0; i < count; i++)
            {
                var invoiceDto = GetInvoiceAnnDto();
                invoiceDto.IssueDate = currentDate;
                var xmlInvoice = mapper.ToXml(invoiceDto);
                await ImportInvoice(xmlInvoice);
                currentDate = currentDate.AddDays(dayStep);
            }
        }

        public async Task<bool> ValidateXmlInvoiceHash(int invoiceId, CancellationToken token = default)
        {
            var invoice = await _indexedDbService.GetInvoice(invoiceId);
            if (invoice?.FinalizeResult == null || string.IsNullOrEmpty(invoice.FinalizeResult.Sha1Hash))
            {
                return false;
            }

            var computedHash = SHA1.HashData(invoice.FinalizeResult.Blob);
            var computedHashBase64 = Convert.ToBase64String(computedHash);

            return string.Equals(computedHashBase64, invoice.FinalizeResult.Sha1Hash, StringComparison.Ordinal);
        }

        public static BlazorInvoiceDto GetInvoiceAnnDto()
        {
            return new()
            {
                GlobalTaxCategory = "S",
                GlobalTaxScheme = "VAT",
                GlobalTax = 19.0,
                Id = "1",
                IssueDate = DateTime.UtcNow,
                InvoiceTypeCode = "380",
                DocumentCurrencyCode = "EUR",
                Note = "Test Note",
                SellerParty = new()
                {
                    Name = "Seller Name",
                    StreetName = "Test Street",
                    City = "Test City",
                    PostCode = "123456",
                    CountryCode = "DE",
                    Telefone = "1234/54321",
                    Email = "seller@example.com",
                    RegistrationName = "Seller Name",
                    TaxId = "DE12345678"
                },
                BuyerParty = new()
                {
                    Name = "Buyer Name",
                    StreetName = "Test Street",
                    City = "Test City",
                    PostCode = "123456",
                    CountryCode = "DE",
                    Telefone = "1234/54321",
                    Email = "buyer@example.com",
                    RegistrationName = "Buyer Name",
                    BuyerReference = "04011000-12345-34",
                },
                PaymentMeans = new()
                {
                    Iban = "DE12 1234 1234 1234 1234 12",
                    Bic = "BICABCDE",
                    Name = "Bank Name",
                    PaymentMeansTypeCode = "30",
                },
                PaymentTermsNote = "Zahlbar innerhalb von 14 Tagen nach Erhalt der Rechnung.",
                PayableAmount = 119.0,
                InvoiceLines = new List<InvoiceLineAnnotationDto>
                {
                    new()
                    {
                        Id = "1",
                        StartDate = new(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 8, 0, 0),
                        EndDate = new(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 9, 0, 0),
                        Quantity = 1.0,
                        QuantityCode = "HUR",
                        UnitPrice = 100.0,
                        Name = "Test Job"
                    }
                }
            };
        }
    }
}