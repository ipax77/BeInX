using Microsoft.EntityFrameworkCore;
using pax.XRechnung.NET.AnnotatedDtos;

namespace BlazorInvoice.Db.Repository;

public partial class InvoiceRepository
{
    public async Task AddReplaceOrDeletePartyLogo(string? base64String, int partyId)
    {
        var seller = await context.InvoiceParties
            .FirstOrDefaultAsync(f => f.InvoicePartyId == partyId);
        ArgumentNullException.ThrowIfNull(seller, nameof(seller));
        if (string.IsNullOrEmpty(base64String))
        {
            seller.LogoReferenceId = null;
            seller.Logo = null;
        }
        else
        {
            seller.LogoReferenceId = Guid.NewGuid().ToString();
            seller.Logo = Convert.FromBase64String(base64String);
        }
        await context.SaveChangesAsync();
    }

    public async Task<DocumentReferenceAnnotationDto?> AddReplaceOrDeletePdf(string? base64String, int invoiceId)
    {
        var invoice = await context.Invoices
            .Include(i => i.AdditionalDocumentReferences)
            .FirstOrDefaultAsync(f => f.InvoiceId == invoiceId);
        ArgumentNullException.ThrowIfNull(invoice, nameof(invoice));

        var pdf = invoice.AdditionalDocumentReferences
            .FirstOrDefault(f => f.MimeCode == "application/pdf");

        if (string.IsNullOrEmpty(base64String))
        {
            if (pdf is not null)
            {
                context.AdditionalDocumentReferences.Remove(pdf);
                await context.SaveChangesAsync();
            }

            return null;
        }

        if (pdf is null)
        {
            pdf = new()
            {
                Id = Guid.NewGuid().ToString(),
                MimeCode = "application/pdf",
                DocumentDescription = "PDF",
                FileName = "Invoice.pdf",
                Content = Convert.FromBase64String(base64String),
                Invoice = invoice,
            };

            context.AdditionalDocumentReferences.Add(pdf);
        }
        else
        {
            pdf.Content = Convert.FromBase64String(base64String);
            context.AdditionalDocumentReferences.Update(pdf);
        }
        await context.SaveChangesAsync();
        return new DocumentReferenceAnnotationDto
        {
            Id = pdf.Id,
            MimeCode = pdf.MimeCode,
            DocumentDescription = pdf.DocumentDescription,
            FileName = pdf.FileName,
            Content = Convert.ToBase64String(pdf.Content),
        };
    }
}