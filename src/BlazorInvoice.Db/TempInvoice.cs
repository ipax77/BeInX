using Microsoft.EntityFrameworkCore;

namespace BlazorInvoice.Db;

public class TempInvoice
{
    public int TempInvoiceId { get; set; }
    [Precision(0)]
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public byte[] InvoiceBlob { get; set; } = [];
    public int? InvoiceId { get; set; }
    public int? SellerPartyId { get; set; }
    public int? BuyerPartyId { get; set; }
    public int? PaymentMeansId { get; set; }
}