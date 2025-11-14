using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;

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

    public new BuyerAnnotationDto BuyerParty
    {
        get => base.BuyerParty;
        set => base.BuyerParty = value;
    }

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
    SellerAnnotationDto, BuyerAnnotationDto, PaymentAnnotationDto, InvoiceLineAnnotationDto>
{
    public BlazorInvoiceMapper()
    : base(
        new DocumentReferenceAnnotationMapper(),
        new InvoiceSellerPartyAnnotationMapper(),
        new InvoiceBuyerPartyAnnotationMapper(),
        new PaymentMeansAnnotationMapper(),
        new InvoiceLineAnnotationMapper()
    )
    {
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