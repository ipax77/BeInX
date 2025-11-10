using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;

namespace beinx.shared;

public class PaymentMeansEntity
{
    public int? Id { get; set; }
    public PaymentAnnotationDto Payment { get; set; } = null!;
}

public class Draft<T>
{
    public string Id { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public T Data { get; set; } = default!;
    public DateTime UpdatedAt { get; set; }
}

public class PartyEntity<TPartyDto> where TPartyDto : IPartyBaseDto
{
    public int? Id { get; set; }
    public TPartyDto Party { get; set; } = default!;
}

public class InvoiceDtoInfo
{
    public BlazorInvoiceDto InvoiceDto { get; set; } = null!;
    public int SellerId { get; set; }
    public int BuyerId { get; set; }
    public int PaymentId { get; set; }
}

public class FinalizeResult
{
    public DateTime XmlInvoiceCreated { get; set; }
    public string XmlInvoiceSha1Hash { get; set; } = string.Empty;
    public byte[] XmlInvoiceBlob { get; set; } = [];
}   

public class InvoiceEntity
{
    public int? Id { get; set; }
    public InvoiceDtoInfo Info{ get; set; } = null!;
    public int Year { get; set; }
    public bool IsPaid { get; set; }
    public bool IsImported { get; set; }
    public FinalizeResult? FinalizeResult { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class InvoiceListItem
{
    public int Id { get; set; }
    public string InvoiceId { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public int Year { get; set; }
}