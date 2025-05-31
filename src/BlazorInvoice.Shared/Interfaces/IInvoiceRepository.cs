using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;
using pax.XRechnung.NET.XmlModels;

namespace BlazorInvoice.Shared.Interfaces;

public interface IInvoiceRepository
{
    Task AddReplaceOrDeletePartyLogo(string? base64String, int partyId);
    Task<DocumentReferenceAnnotationDto?> AddReplaceOrDeletePdf(string? base64String, int invoiceId);
    Task<DocumentReferenceAnnotationDto?> AddReplaceOrDeleteSellerLogo(int invoiceId, CancellationToken token);
    Task<int> CreateInvoice(BlazorInvoiceDto invoiceDto, int sellerId, int buyerId, int paymentId, bool isImported = false, CancellationToken token = default);
    Task<int> CreateParty(IPartyBaseDto party, bool isSeller, CancellationToken token = default);
    Task<int> CreatePaymentMeans(IPaymentMeansBaseDto paymentMeans, CancellationToken token = default);
    Task DeleteInvoice(int invoiceId, CancellationToken token = default);
    Task DeleteParty(int partyId, CancellationToken token = default);
    Task DeletePaymentMeans(int paymentMeansId, CancellationToken token = default);
    Task<List<PartyListDto>> GetBuyers(InvoiceListRequest request, CancellationToken token = default);
    Task<int> GetBuyersCount(InvoiceListRequest request, CancellationToken token = default);
    Task<InvoiceDtoInfo?> GetInvoice(int invoiceId, CancellationToken token = default);
    Task<List<InvoiceListDto>> GetInvoices(InvoiceListRequest request, CancellationToken token = default);
    Task<int> GetInvoicesCount(InvoiceListRequest request, CancellationToken token = default);
    Task<SellerAnnotationDto?> GetSeller(int partyId, CancellationToken token = default);
    Task<BuyerAnnotationDto?> GetBuyer(int partyId, CancellationToken token = default);
    Task<DocumentReferenceAnnotationDto?> GetPartyLogo(int partyId, CancellationToken token = default);
    Task<PaymentAnnotationDto> GetPaymentMeans(int paymentMeansId, CancellationToken token = default);
    Task<List<PaymentListDto>> GetPayments(InvoiceListRequest request, CancellationToken token = default);
    Task<int> GetPaymentsCount(InvoiceListRequest request, CancellationToken token = default);
    Task<List<PartyListDto>> GetSellers(InvoiceListRequest request, CancellationToken token = default);
    Task<int> GetSellersCount(InvoiceListRequest request, CancellationToken token = default);
    Task UpdateInvoice(int invoiceId, BlazorInvoiceDto invoiceDto, CancellationToken token = default);
    Task UpdateParty(int partyId, IPartyBaseDto party, CancellationToken token = default);
    Task UpdatePaymentMeans(int paymentMeansId, IPaymentMeansBaseDto paymentMeans, CancellationToken token = default);
    Task<FinalizeResult> FinalizeInvoice(int invoiceId, XmlInvoice xmlInvoice, CancellationToken token = default);
    Task<XmlInvoice?> GetXmlInvoice(int invoiceId, CancellationToken token = default);
    Task<bool> ValidateXmlInvoiceHash(int invoiceId, CancellationToken token = default);
    Task<bool> HasTempInvoice();
    Task DeleteTempInvoice();
    Task SaveTempInvoice(InvoiceDtoInfo request);
    Task<InvoiceDtoInfo?> GetTempInvoice();
    Task<int> ImportInvoice(XmlInvoice invoice, CancellationToken token = default);
    Task<ExportResult> ExportInvoice(int invoiceId);
    Task SetIsPaid(int invoiceId, bool isPaid, CancellationToken token = default);
    Task<int> CreateInvoiceCopy(int invoiceId);
    Task SeedTestInvoices(int count);
    Task<int> SeedTestInvoice();
}