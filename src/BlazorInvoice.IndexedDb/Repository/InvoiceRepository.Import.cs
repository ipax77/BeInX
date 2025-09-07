using BlazorInvoice.Shared;
using Microsoft.EntityFrameworkCore;
using pax.XRechnung.NET;
using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.XmlModels;

namespace BlazorInvoice.Db.Repository;

public partial class InvoiceRepository
{
    public async Task<int> ImportInvoice(XmlInvoice invoice, CancellationToken token = default)
    {
        var validationResult = XmlInvoiceValidator.Validate(invoice);
        if (!validationResult.IsValid)
        {
            return 0; // TODO result invo
        }
        var mapper = new BlazorInvoiceMapper();
        var dto = mapper.FromXml(invoice);

        // TODO handle AdditionalDocumentReferences

        int sellerId = await IdentifyOrCreateSeller(dto.SellerParty, token);
        int buyerId = await IdentifyOrCreateBuyer(dto.BuyerParty, token);
        int paymentId = await IdentifyOrCreatePayment(dto.PaymentMeans, token);
        int invoiceId = await CreateInvoice(dto, sellerId, buyerId, paymentId, true, token);
        await FinalizeInvoice(invoiceId, invoice);
        return invoiceId;
    }

    private async Task<int> IdentifyOrCreatePayment(PaymentAnnotationDto paymentMeans, CancellationToken token)
    {
        var existing = await context.PaymentMeans
            .FirstOrDefaultAsync(f =>
                f.Iban == paymentMeans.Iban, token);

        if (existing != null)
            return existing.PaymentMeansId;

        return await CreatePaymentMeans(paymentMeans, token);
    }

    private async Task<int> IdentifyOrCreateBuyer(BuyerAnnotationDto buyerParty, CancellationToken token)
    {
        var existing = await context.InvoiceParties.FirstOrDefaultAsync(s =>
            !s.IsSeller &&
            s.Name == buyerParty.Name &&
            s.RegistrationName == buyerParty.RegistrationName &&
            s.CountryCode == buyerParty.CountryCode, token);

        if (existing != null)
            return existing.InvoicePartyId;

        return await CreateParty(buyerParty, isSeller: false, token);
    }

    private async Task<int> IdentifyOrCreateSeller(SellerAnnotationDto sellerParty, CancellationToken token)
    {
        var existing = await context.InvoiceParties.FirstOrDefaultAsync(s =>
            s.IsSeller &&
            s.Name == sellerParty.Name &&
            s.TaxId == sellerParty.TaxId &&
            s.CountryCode == sellerParty.CountryCode, token);

        if (existing != null)
            return existing.InvoicePartyId;

        return await CreateParty(sellerParty, isSeller: true, token);
    }
}

