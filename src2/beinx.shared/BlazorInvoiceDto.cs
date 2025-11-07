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