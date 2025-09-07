using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;
using pax.XRechnung.NET.XmlModels;

namespace BlazorInvoice.IndexedDb.Services;

public partial class InvoiceRepository(IIndexedDbService indexedDbService, ILogger<InvoiceRepository> logger) : IInvoiceRepository
{
    // Payments
    public Task<int> CreatePaymentMeans(IPaymentMeansBaseDto paymentMeans, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task DeletePaymentMeans(int paymentMeansId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<PaymentAnnotationDto> GetPaymentMeans(int paymentMeansId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<PaymentListDto>> GetPayments(InvoiceListRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetPaymentsCount(InvoiceListRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdatePaymentMeans(int paymentMeansId, IPaymentMeansBaseDto paymentMeans, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
    // End Payments

    public Task<int> CreateInvoice(BlazorInvoiceDto invoiceDto, int sellerId, int buyerId, int paymentId, bool isImported = false, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task AddReplaceOrDeletePartyLogo(string? base64String, int partyId)
    {
        throw new NotImplementedException();
    }

    public Task<DocumentReferenceAnnotationDto?> AddReplaceOrDeletePdf(string? base64String, int invoiceId)
    {
        throw new NotImplementedException();
    }

    public Task<DocumentReferenceAnnotationDto?> AddReplaceOrDeleteSellerLogo(int invoiceId, CancellationToken token)
    {
        throw new NotImplementedException();
    }


    public Task<int> CreateInvoiceCopy(int invoiceId)
    {
        throw new NotImplementedException();
    }

    public Task<int> CreateParty(IPartyBaseDto party, bool isSeller, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }


    public Task DeleteInvoice(int invoiceId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteParty(int partyId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }


    public Task DeleteTempInvoice()
    {
        throw new NotImplementedException();
    }

    public Task<ExportResult> ExportInvoice(int invoiceId)
    {
        throw new NotImplementedException();
    }

    public Task<FinalizeResult> FinalizeInvoice(int invoiceId, XmlInvoice xmlInvoice, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<BuyerAnnotationDto?> GetBuyer(int partyId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<PartyListDto>> GetBuyers(InvoiceListRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetBuyersCount(InvoiceListRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<InvoiceDtoInfo?> GetInvoice(int invoiceId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<InvoiceListDto>> GetInvoices(InvoiceListRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetInvoicesCount(InvoiceListRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<DocumentReferenceAnnotationDto?> GetPartyLogo(int partyId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }




    public Task<SellerAnnotationDto?> GetSeller(int partyId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<PartyListDto>> GetSellers(InvoiceListRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetSellersCount(InvoiceListRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<InvoiceDtoInfo?> GetTempInvoice()
    {
        throw new NotImplementedException();
    }

    public Task<XmlInvoice?> GetXmlInvoice(int invoiceId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HasTempInvoice()
    {
        throw new NotImplementedException();
    }

    public Task<int> ImportInvoice(XmlInvoice invoice, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task SaveTempInvoice(InvoiceDtoInfo request)
    {
        throw new NotImplementedException();
    }

    public Task<int> SeedTestInvoice()
    {
        throw new NotImplementedException();
    }

    public Task SeedTestInvoices(int count)
    {
        throw new NotImplementedException();
    }

    public Task SetIsPaid(int invoiceId, bool isPaid, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateInvoice(int invoiceId, BlazorInvoiceDto invoiceDto, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateParty(int partyId, IPartyBaseDto party, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }


    public Task<bool> ValidateXmlInvoiceHash(int invoiceId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}