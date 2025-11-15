using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.XmlModels;

namespace beinx.shared.Interfaces;

public interface IInvoiceService
{
    Task<DocumentReferenceAnnotationDto?> AddReplaceOrDeletePdf(string? base64String, int invoiceId);
    Task<ExportResult> ExportInvoice(int invoiceId);
    Task<FinalizeResult> FinalizeInvoice(int invoiceId, XmlInvoice xmlInvoice);
    Task<StatsResponse> GetStats(int year);
    Task<ImportResult> ImportInvoice(BlazorInvoiceDto invoiceDto);
    BlazorInvoiceDto? GetDtoFromZugferdXmlString(string xml);
    string GetZugferdXmlString(BlazorInvoiceDto dto);
    Task<ImportResult> ImportSampleDto();
}