using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorInvoice.Db;

public sealed class Invoice
{
    [Key]
    public int InvoiceId { get; set; }
    [MaxLength(4)]
    public string GlobalTaxCategory { get; set; } = "S";
    [MaxLength(4)]
    public string GlobalTaxScheme { get; set; } = "VAT";
    public double GlobalTax { get; set; } = 19.0;
    [MaxLength(20)]
    public string Id { get; set; } = string.Empty;
    [Precision(0)]
    public DateTime IssueDate { get; set; }
    [Precision(0)]
    public DateTime? DueDate { get; set; }
    [MaxLength(4)]
    public string InvoiceTypeCode { get; set; } = "380";
    public string? Note { get; set; }
    [MaxLength(4)]
    public string DocumentCurrencyCode { get; set; } = "EUR";


    public string PaymentTermsNote { get; set; } = string.Empty;
    public double PayableAmount { get; set; }
    public int SellerPartyId { get; set; }
    public bool IsImported { get; set; }
    [ForeignKey("SellerPartyId")]
    public InvoiceParty? SellerParty { get; set; }
    public int BuyerPartyId { get; set; }
    [ForeignKey("BuyerPartyId")]
    public InvoiceParty? BuyerParty { get; set; }
    public int PaymentMeansId { get; set; }
    public PaymentMeans? PaymentMeans { get; set; }
    public ICollection<InvoiceLine> InvoiceLines { get; set; } = [];
    public ICollection<AdditionalDocumentReference> AdditionalDocumentReferences { get; set; } = [];
    public DateTime? XmlInvoiceCreated { get; set; }
    public string? XmlInvoiceSha1Hash { get; set; }
    public byte[]? XmlInvoiceBlob { get; set; } = [];
    public bool IsPaid { get; set; }
    public decimal TotalAmountWithoutVat { get; set; }
}

public sealed class AdditionalDocumentReference
{
    [Key]
    public int AdditionalDocumentReferenceId { get; set; }
    [MaxLength(20)]
    public string Id { get; set; } = string.Empty;
    [MaxLength(50)]
    public string MimeCode { get; set; } = string.Empty;
    public string DocumentDescription { get; set; } = string.Empty;
    [MaxLength(50)]
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
    public int InvoiceId { get; set; }
    [ForeignKey("InvoiceId")]
    public Invoice? Invoice { get; set; }
}

public sealed class InvoiceLine
{
    [Key]
    public int InvoiceLineId { get; set; }
    [MaxLength(10)]
    public string Id { get; set; } = string.Empty;
    public string? Note { get; set; }
    public double Quantity { get; set; }
    [MaxLength(5)]
    public string QuantityCode { get; set; } = "HUR";
    public double UnitPrice { get; set; }
    [Precision(0)]
    public DateTime? StartDate { get; set; }
    [Precision(0)]
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
}

public sealed class PaymentMeans
{
    [Key]
    public int PaymentMeansId { get; set; }
    [MaxLength(22)]
    public string Iban { get; set; } = string.Empty;
    [MaxLength(12)]
    public string Bic { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(4)]
    public string PaymentMeansTypeCode { get; set; } = "30";
    public bool IsDeleted { get; set; }
    public ICollection<Invoice> Invoices { get; set; } = [];
}


public sealed class InvoiceParty
{
    [Key]
    public int InvoicePartyId { get; set; }
    [MaxLength(100)]
    public string? Website { get; set; }
    [MaxLength(20)]
    public string? LogoReferenceId { get; set; }
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(100)]
    public string? StreetName { get; set; }
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;
    [MaxLength(12)]
    public string PostCode { get; set; } = string.Empty;
    [MaxLength(4)]
    public string CountryCode { get; set; } = "DE";
    [MaxLength(20)]
    public string Telefone { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    [MaxLength(100)]
    public string RegistrationName { get; set; } = string.Empty;
    [MaxLength(100)]
    public string TaxId { get; set; } = string.Empty;
    [MaxLength(20)]
    public string BuyerReference { get; set; } = string.Empty;
    [MaxLength(100)]
    public string? CompanyId { get; set; } = string.Empty;
    public byte[]? Logo { get; set; }
    public bool IsSeller { get; set; }
    public bool IsDeleted { get; set; }
    public decimal HourlyRate { get; set; }
}