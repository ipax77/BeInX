using BlazorInvoice.Shared;
using Microsoft.EntityFrameworkCore;
using pax.XRechnung.NET;
using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.XmlModels;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace BlazorInvoice.Db.Repository;

public partial class InvoiceRepository
{
    public async Task<InvoiceDtoInfo?> GetInvoice(int invoiceId, CancellationToken token = default)
    {
        var invoice = await context.Invoices
            .Include(i => i.InvoiceLines)
            .Include(i => i.AdditionalDocumentReferences)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, token);

        if (invoice == null) return null;

        var dto = new BlazorInvoiceDto
        {
            GlobalTaxCategory = invoice.GlobalTaxCategory,
            GlobalTaxScheme = invoice.GlobalTaxScheme,
            GlobalTax = invoice.GlobalTax,
            Id = invoice.Id,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            Note = invoice.Note,
            InvoiceTypeCode = invoice.InvoiceTypeCode,
            DocumentCurrencyCode = invoice.DocumentCurrencyCode,
            PaymentTermsNote = invoice.PaymentTermsNote,
            PayableAmount = invoice.PayableAmount,
            InvoiceLines = [.. invoice.InvoiceLines
                .Select(line => new InvoiceLineAnnotationDto()
                {
                    Id = line.Id,
                    Note = line.Note,
                    Quantity = line.Quantity,
                    QuantityCode = line.QuantityCode,
                    UnitPrice = line.UnitPrice,
                    StartDate = line.StartDate,
                    EndDate = line.EndDate,
                    Description = line.Description,
                    Name = line.Name
                })],
            AdditionalDocumentReferences = [.. invoice.AdditionalDocumentReferences
                .Select(doc => new DocumentReferenceAnnotationDto()
                {
                    Id = doc.Id,
                    DocumentDescription = doc.DocumentDescription,
                    MimeCode = doc.MimeCode,
                    FileName = doc.FileName,
                    Content = Convert.ToBase64String(doc.Content),
                })],
            SellerParty = await GetSeller(invoice.SellerPartyId, token) ?? new(),
            BuyerParty = await GetBuyer(invoice.BuyerPartyId, token) ?? new(),
            PaymentMeans = await GetPaymentMeans(invoice.PaymentMeansId, token) ?? new()
        };

        if (!string.IsNullOrEmpty(dto.SellerParty.LogoReferenceId))
        {
            var sellerLogo = await GetPartyLogo(invoice.SellerPartyId);
            dto.EmbedSellerLogo(sellerLogo);
        }

        return new InvoiceDtoInfo()
        {
            InvoiceDto = dto,
            InvoiceId = invoice.InvoiceId,
            SellerId = invoice.SellerPartyId,
            BuyerId = invoice.BuyerPartyId,
            PaymentId = invoice.PaymentMeansId,
        };
    }

    public async Task<int> CreateInvoice(BlazorInvoiceDto invoiceDto,
                                         int sellerId,
                                         int buyerId,
                                         int paymentId,
                                         bool isImported = false,
                                         CancellationToken token = default)
    {
        Invoice invoice = new()
        {
            GlobalTaxCategory = invoiceDto.GlobalTaxCategory,
            GlobalTaxScheme = invoiceDto.GlobalTaxScheme,
            GlobalTax = invoiceDto.GlobalTax,
            Id = invoiceDto.Id,
            IssueDate = invoiceDto.IssueDate,
            DueDate = invoiceDto.DueDate,
            Note = invoiceDto.Note,
            InvoiceTypeCode = invoiceDto.InvoiceTypeCode,
            DocumentCurrencyCode = invoiceDto.DocumentCurrencyCode,
            PaymentTermsNote = invoiceDto.PaymentTermsNote,
            PayableAmount = invoiceDto.PayableAmount,
            InvoiceLines = invoiceDto.InvoiceLines.Select(line => new InvoiceLine()
            {
                Id = line.Id,
                Note = line.Note,
                Quantity = line.Quantity,
                QuantityCode = line.QuantityCode,
                UnitPrice = line.UnitPrice,
                StartDate = line.StartDate,
                EndDate = line.EndDate,
                Description = line.Description,
                Name = line.Name,
            }).ToList(),
            SellerPartyId = sellerId,
            BuyerPartyId = buyerId,
            PaymentMeansId = paymentId,
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync(token);
        return invoice.InvoiceId;
    }

    public async Task UpdateInvoice(int invoiceId, BlazorInvoiceDto invoiceDto, CancellationToken token = default)
    {
        var invoice = await context.Invoices
            .Include(i => i.InvoiceLines)
            .Include(i => i.AdditionalDocumentReferences)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, token);

        ArgumentNullException.ThrowIfNull(invoice, nameof(invoice));

        invoice.Id = invoiceDto.Id;
        invoice.GlobalTaxCategory = invoiceDto.GlobalTaxCategory;
        invoice.GlobalTaxScheme = invoiceDto.GlobalTaxScheme;
        invoice.GlobalTax = invoiceDto.GlobalTax;
        invoice.IssueDate = invoiceDto.IssueDate;
        invoice.DueDate = invoiceDto.DueDate;
        invoice.Note = invoiceDto.Note;
        invoice.InvoiceTypeCode = invoiceDto.InvoiceTypeCode;
        invoice.DocumentCurrencyCode = invoiceDto.DocumentCurrencyCode;
        invoice.PaymentTermsNote = invoiceDto.PaymentTermsNote;
        invoice.PayableAmount = invoiceDto.PayableAmount;

        invoice.InvoiceLines.Clear();
        invoice.InvoiceLines = invoiceDto.InvoiceLines.Select(line => new InvoiceLine()
        {
            Id = line.Id,
            Note = line.Note,
            Quantity = line.Quantity,
            QuantityCode = line.QuantityCode,
            UnitPrice = line.UnitPrice,
            StartDate = line.StartDate,
            EndDate = line.EndDate,
            Description = line.Description,
            Name = line.Name,
        }).ToList();
        await context.SaveChangesAsync(token);
    }

    public async Task DeleteInvoice(int invoiceId, CancellationToken token = default)
    {
        var invoice = await context.Invoices
            .Include(i => i.InvoiceLines)
            .Include(i => i.AdditionalDocumentReferences)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, token);

        ArgumentNullException.ThrowIfNull(invoice, nameof(invoice));

        context.Invoices.Remove(invoice);
        await context.SaveChangesAsync(token);
    }

    public async Task<DocumentReferenceAnnotationDto?> AddReplaceOrDeleteSellerLogo(int invoiceId, CancellationToken token)
    {
        var invoice = await context.Invoices
            .Include(i => i.SellerParty)
            .Include(i => i.AdditionalDocumentReferences)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        if (invoice is null || invoice.SellerParty is null)
        {
            return null;
        }
        var desc = "Seller Logo";
        var logoId = invoice.SellerParty.LogoReferenceId ?? string.Empty;
        var base64String = invoice.SellerParty.Logo is null ? null : Convert.ToBase64String(invoice.SellerParty.Logo);
        var docRef = invoice.AdditionalDocumentReferences
            .FirstOrDefault(f => f.DocumentDescription == desc);

        if (string.IsNullOrEmpty(base64String))
        {
            if (docRef is not null)
            {
                context.AdditionalDocumentReferences.Remove(docRef);
                await context.SaveChangesAsync(token);
            }
            return null;
        }
        else
        {
            if (docRef is null)
            {
                docRef = new()
                {
                    Id = logoId,
                    MimeCode = "image/png",
                    DocumentDescription = desc,
                    FileName = "SellerLogo.png",
                    Content = Convert.FromBase64String(base64String),
                    Invoice = invoice,
                };
                context.AdditionalDocumentReferences.Add(docRef);
            }
            else
            {
                docRef.Content = Convert.FromBase64String(base64String);
                context.AdditionalDocumentReferences.Update(docRef);
            }
            await context.SaveChangesAsync(token);
        }

        return new DocumentReferenceAnnotationDto()
        {
            Id = docRef.Id,
            DocumentDescription = docRef.DocumentDescription,
            MimeCode = docRef.MimeCode,
            FileName = docRef.FileName,
            Content = Convert.ToBase64String(docRef.Content),
        };
    }

    public async Task<FinalizeResult> FinalizeInvoice(int invoiceId, XmlInvoice xmlInvoice, CancellationToken token = default)
    {
        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, token);

        ArgumentNullException.ThrowIfNull(invoice, nameof(invoice));

        var xmlText = XmlInvoiceWriter.Serialize(xmlInvoice);
        ArgumentNullException.ThrowIfNull(xmlText, nameof(xmlText));

        var bytes = Encoding.UTF8.GetBytes(xmlText);
        var hash = SHA1.HashData(bytes);

        invoice.XmlInvoiceCreated = DateTime.UtcNow;
        invoice.XmlInvoiceSha1Hash = Convert.ToBase64String(hash);
        invoice.XmlInvoiceBlob = bytes;
        invoice.TotalAmountWithoutVat = xmlInvoice.LegalMonetaryTotal.TaxExclusiveAmount.Value;
        await context.SaveChangesAsync(token);
        return new(invoice.XmlInvoiceCreated.Value, invoice.XmlInvoiceSha1Hash, invoice.XmlInvoiceBlob);
    }

    public async Task<XmlInvoice?> GetXmlInvoice(int invoiceId, CancellationToken token = default)
    {
        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, token);

        ArgumentNullException.ThrowIfNull(invoice, nameof(invoice));

        if (invoice.XmlInvoiceBlob == null) return null;

        var serializer = new XmlSerializer(typeof(XmlInvoice));
        using var stream = new MemoryStream(invoice.XmlInvoiceBlob);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var xmlInvoice = (XmlInvoice?)serializer.Deserialize(reader);
        return xmlInvoice;
    }

    public async Task<bool> ValidateXmlInvoiceHash(int invoiceId, CancellationToken token = default)
    {
        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, token);

        ArgumentNullException.ThrowIfNull(invoice, nameof(invoice));

        if (invoice.XmlInvoiceBlob == null || string.IsNullOrEmpty(invoice.XmlInvoiceSha1Hash))
        {
            return false;
        }

        var computedHash = SHA1.HashData(invoice.XmlInvoiceBlob);
        var computedHashBase64 = Convert.ToBase64String(computedHash);

        return string.Equals(computedHashBase64, invoice.XmlInvoiceSha1Hash, StringComparison.Ordinal);
    }

    public async Task SetIsPaid(int invoiceId, bool isPaid, CancellationToken token = default)
    {
        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, token);

        ArgumentNullException.ThrowIfNull(invoice, nameof(invoice));
        invoice.IsPaid = isPaid;
        await context.SaveChangesAsync(token);
        if (invoice.XmlInvoiceCreated == null)
        {
            var info = await GetInvoice(invoiceId, token);
            if (info is null)
            {
                return;
            }
            var mapper = new BlazorInvoiceMapper();
            var xmlInvoice = mapper.ToXml(info.InvoiceDto);
            await FinalizeInvoice(invoiceId, xmlInvoice, token);
        }
    }

    public async Task<int> CreateInvoiceCopy(int invoiceId)
    {
        var invoiceInfo = await GetInvoice(invoiceId);
        ArgumentNullException.ThrowIfNull(invoiceInfo, nameof(invoiceInfo));

        invoiceInfo.InvoiceDto.Id = invoiceInfo.InvoiceDto.Id + "_copy";
        invoiceInfo.InvoiceDto.IssueDate = DateTime.UtcNow;
        invoiceInfo.InvoiceDto.DueDate = DateTime.UtcNow.AddDays(14);
        invoiceInfo.InvoiceDto.InvoiceLines.Clear();
        invoiceInfo.InvoiceDto.AdditionalDocumentReferences.Clear();

        if (invoiceInfo.SellerId != null && !string.IsNullOrEmpty(invoiceInfo.InvoiceDto.SellerParty.LogoReferenceId))
        {
            var sellerLogo = await GetPartyLogo(invoiceInfo.SellerId.Value);
            invoiceInfo.InvoiceDto.EmbedSellerLogo(sellerLogo);
        }

        var newInvoiceId = await CreateInvoice(invoiceInfo.InvoiceDto, invoiceInfo.SellerId ?? 0, invoiceInfo.BuyerId ?? 0, invoiceInfo.PaymentId ?? 0);
        return newInvoiceId;
    }
}