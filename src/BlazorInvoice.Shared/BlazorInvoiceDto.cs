using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;

namespace BlazorInvoice.Shared;

public class BlazorInvoiceDto : InvoiceAnnotationDto
{
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
}