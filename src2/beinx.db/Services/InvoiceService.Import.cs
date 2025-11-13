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

            await invoiceRepository.CreateAsync(info);
            return new(info, null);
        }
        catch (Exception ex)
        {
            return new(null, ex.Message);
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
}
