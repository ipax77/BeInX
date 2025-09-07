
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

namespace BlazorInvoice.IndexedDb.Services
{
    public partial class InvoiceRepository : IInvoiceRepository
    {
        private readonly IIndexedDbService _indexedDbService;
        private readonly ILogger<InvoiceRepository> _logger;

        public InvoiceRepository(IIndexedDbService indexedDbService, ILogger<InvoiceRepository> logger)
        {
            _indexedDbService = indexedDbService;
            _logger = logger;
        }

        // TempInvoice
        public Task DeleteTempInvoice()
        {
            throw new NotImplementedException();
        }

        public Task<InvoiceDtoInfo?> GetTempInvoice()
        {
            throw new NotImplementedException();
        }

        public Task<bool> HasTempInvoice()
        {
            throw new NotImplementedException();
        }

        public Task SaveTempInvoice(InvoiceDtoInfo request)
        {
            throw new NotImplementedException();
        }
        // End TempInvoice

        public Task<ExportResult> ExportInvoice(int invoiceId)
        {
            throw new NotImplementedException();
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
