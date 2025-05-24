namespace BlazorInvoice.Shared;

public class InvoiceDtoInfo
{
    public required BlazorInvoiceDto InvoiceDto { get; init; }
    public required int InvoiceId { get; init; }
    public required int? SellerId { get; init; }
    public required int? BuyerId { get; init; }
    public required int? PaymentId { get; init; }
}
