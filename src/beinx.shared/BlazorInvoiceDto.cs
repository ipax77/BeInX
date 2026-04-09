using System.ComponentModel.DataAnnotations;
using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;
using pax.XRechnung.NET.XmlModels;

namespace beinx.shared;

public class BlazorInvoiceDto : InvoiceAnnotationDto
{
    public new PaymentAnnotationDto PaymentMeans
    {
        get => base.PaymentMeans;
        set => base.PaymentMeans = value;
    }

    public new SellerAnnotationDto SellerParty
    {
        get => base.SellerParty;
        set => base.SellerParty = value;
    }

    public new BlazorBuyerAnnotationDto BuyerParty { get; set; } = new();

    public new List<InvoiceLineAnnotationDto> InvoiceLines
    {
        get => base.InvoiceLines;
        set => base.InvoiceLines = value;
    }

    public new List<DocumentReferenceAnnotationDto> AdditionalDocumentReferences
    {
        get => base.AdditionalDocumentReferences;
        set => base.AdditionalDocumentReferences = value;
    }
}

public class BlazorInvoiceMapper : InvoiceMapperBase<BlazorInvoiceDto, DocumentReferenceAnnotationDto,
    SellerAnnotationDto, BlazorBuyerAnnotationDto, PaymentAnnotationDto, InvoiceLineAnnotationDto>
{
    public BlazorInvoiceMapper()
    : base(
        new DocumentReferenceAnnotationMapper(),
        new InvoiceSellerPartyAnnotationMapper(),
        new BlazorInvoiceBuyerPartyAnnotationMapper(),
        new PaymentMeansAnnotationMapper(),
        new InvoiceLineAnnotationMapper()
    )
    {
    }
}

public class BlazorBuyerAnnotationDto : IPartyBaseDto
{
    public string? Website { get; set; }
    public string? LogoReferenceId { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string? StreetName { get; set; }
    public string? AdditionalStreetName { get; set; }
    [Required]
    public string City { get; set; } = string.Empty;
    [Required]
    public string PostCode { get; set; } = string.Empty;
    [Required]
    [ValidCode(CodeListType.Country_Codes_8)]
    public string CountryCode { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string RegistrationName { get; set; } = string.Empty;
    [Required]
    public string TaxId { get; set; } = string.Empty;
    public string? CompanyId { get; set; }
    public string BuyerReference { get; set; } = string.Empty;
}

public class BlazorInvoiceBuyerPartyAnnotationMapper : InvoiceBuyerPartyMapperBase<BlazorBuyerAnnotationDto>
{
    public override BlazorBuyerAnnotationDto FromXml(XmlParty xmlParty)
    {
        var dto = base.FromXml(xmlParty);
        var myDto = (BlazorBuyerAnnotationDto)dto;
        myDto.AdditionalStreetName = xmlParty.PostalAddress.AdditionalStreetName;
        return myDto;
    }

    public override XmlParty ToXml(IPartyBaseDto partyBaseDto)
    {
        var xml = base.ToXml(partyBaseDto);
        if (partyBaseDto is BlazorBuyerAnnotationDto annotationDto)
        {
            xml.PostalAddress.AdditionalStreetName = InvoiceMapperUtils.GetNullableString(annotationDto.AdditionalStreetName);
        }
        return xml;
    }
}

public static class BlazorInvoiceDtoExtensions
{
    public static void EmbedPdf(this BlazorInvoiceDto invoice, DocumentReferenceAnnotationDto? doc)
    {
        var existingDoc = invoice.AdditionalDocumentReferences.FirstOrDefault(d => d.FileName == "Invoice.pdf");
        if (existingDoc != null)
        {
            invoice.AdditionalDocumentReferences.Remove(existingDoc);
        }
        if (doc != null)
        {
            invoice.AdditionalDocumentReferences.Add(doc);
        }
    }

    public static void EmbedSellerLogo(this BlazorInvoiceDto invoice, DocumentReferenceAnnotationDto? doc)
    {
        var existingDoc = invoice.AdditionalDocumentReferences.FirstOrDefault(d => d.Id == doc?.Id);
        if (existingDoc != null)
        {
            invoice.AdditionalDocumentReferences.Remove(existingDoc);
        }
        if (doc != null)
        {
            invoice.AdditionalDocumentReferences.Add(doc);
            invoice.SellerParty.LogoReferenceId = doc.Id;
        }
        else
        {
            invoice.SellerParty.LogoReferenceId = null;
        }
    }

    public static void UpdateAmount(this BlazorInvoiceDto invoice)
    {
        decimal taxRate = (decimal)invoice.GlobalTax / 100.0m;
        decimal taxExclusiveAmount = Math.Round((decimal)invoice.InvoiceLines.Sum(s => s.LineTotal), 2);
        decimal payableAmount = Math.Round(taxExclusiveAmount + taxExclusiveAmount * taxRate, 2);
        invoice.PayableAmount = (double)payableAmount;

    }

    public static void SetLineIds(this BlazorInvoiceDto invoice)
    {
        for (int i = 0; i < invoice.InvoiceLines.Count; i++)
        {
            invoice.InvoiceLines[i].Id = (i + 1).ToString();
        }
    }
}