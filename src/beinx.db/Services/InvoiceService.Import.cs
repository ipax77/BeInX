using beinx.shared;
using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;

namespace beinx.db.Services;

public partial class InvoiceService
{
    public async Task<ImportResult> ImportInvoice(BlazorInvoiceDto invoiceDto)
    {
        try
        {
            InvoiceDtoInfo info = new()
            {
                InvoiceDto = invoiceDto,
                SellerId = await GetSellerId(invoiceDto.SellerParty),
                BuyerId = await GetBuyerId(invoiceDto.BuyerParty),
                PaymentId = await GetPaymentId(invoiceDto.PaymentMeans)
            };

            var id = await invoiceRepository.CreateAsync(info, true);
            return new(info, id, null);
        }
        catch (Exception ex)
        {
            return new(null, 0, ex.Message);
        }
    }

    private async Task<int> GetSellerId(SellerAnnotationDto seller)
    {
        var sellers = await sellerRepository.GetAllAsync();

        var duplicate = sellers.FirstOrDefault(s =>
            IsSameParty(s.Party, seller));

        if (duplicate?.Id != null)
        {
            return duplicate.Id.Value;
        }

        return await sellerRepository.CreateAsync(seller);
    }

    private async Task<int> GetBuyerId(BuyerAnnotationDto buyer)
    {
        var buyers = await buyerRepository.GetAllAsync();

        var duplicate = buyers.FirstOrDefault(b =>
            IsSameParty(b.Party, buyer));

        if (duplicate?.Id != null)
        {
            return duplicate.Id.Value;
        }

        return await buyerRepository.CreateAsync(buyer);
    }

    private static bool IsSameParty(IPartyBaseDto existing, IPartyBaseDto incoming)
    {
        // Check TaxId (strongest identifier)
        if (!string.IsNullOrWhiteSpace(existing.TaxId) &&
            !string.IsNullOrWhiteSpace(incoming.TaxId) &&
            existing.TaxId.Equals(incoming.TaxId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check CompanyId
        if (!string.IsNullOrWhiteSpace(existing.CompanyId) &&
            !string.IsNullOrWhiteSpace(incoming.CompanyId) &&
            existing.CompanyId.Equals(incoming.CompanyId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check Name + Email combination (weaker, but useful fallback)
        if (!string.IsNullOrWhiteSpace(existing.Email) &&
            !string.IsNullOrWhiteSpace(incoming.Email) &&
            existing.Name.Equals(incoming.Name, StringComparison.OrdinalIgnoreCase) &&
            existing.Email.Equals(incoming.Email, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private async Task<int> GetPaymentId(PaymentAnnotationDto payment)
    {
        var payments = await paymentsRepository.GetAllAsync();

        var duplicate = payments.FirstOrDefault(p =>
            p.Payment.Iban.Equals(payment.Iban, StringComparison.OrdinalIgnoreCase) &&
            p.Payment.Bic.Equals(payment.Bic, StringComparison.OrdinalIgnoreCase));

        if (duplicate?.Id != null)
        {
            return duplicate.Id.Value;
        }

        return await paymentsRepository.CreateAsync(payment);
    }

    public BlazorInvoiceDto? GetDtoFromZugferdXmlString(string xml)
    {
        return ZugferdMapper.MapFromZugferd(xml);
    }

    public string GetZugferdXmlString(BlazorInvoiceDto dto)
    {
        return ZugferdMapper.MapToZugferd(dto);
    }

    public async Task<ImportResult> ImportSampleDto()
    {
        var invoiceDto = GetSampleDto();
        try
        {
            InvoiceDtoInfo info = new()
            {
                InvoiceDto = invoiceDto,
                SellerId = await GetSellerId(invoiceDto.SellerParty),
                BuyerId = await GetBuyerId(invoiceDto.BuyerParty),
                PaymentId = await GetPaymentId(invoiceDto.PaymentMeans)
            };

            var id = await invoiceRepository.CreateAsync(info);
            return new(info, id, null);
        }
        catch (Exception ex)
        {
            return new(null, 0, ex.Message);
        }
    }

    private static BlazorInvoiceDto GetSampleDto()
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
                TaxId = "DE12345678",
                CompanyId = "000/000/0000 0",
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
            InvoiceLines = [
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
            ]
        };
    }
}
