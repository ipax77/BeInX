
using BlazorInvoice.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BlazorInvoice.Db.Repository;

public partial class InvoiceRepository
{
    public async Task<bool> HasTempInvoice()
    {
        var count = await context.TempInvoices.CountAsync();
        return count > 0;
    }

    public async Task DeleteTempInvoice()
    {
        var tempInvoice = await context.TempInvoices.FirstOrDefaultAsync();
        if (tempInvoice != null)
        {
            context.TempInvoices.Remove(tempInvoice);
            await context.SaveChangesAsync();
        }
    }

    public async Task SaveTempInvoice(InvoiceDtoInfo request)
    {
        var json = JsonSerializer.Serialize(request.InvoiceDto);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var tempInvoice = await context.TempInvoices.FirstOrDefaultAsync();
        if (tempInvoice is null)
        {
            tempInvoice = new TempInvoice
            {
                Created = DateTime.UtcNow,
            };
            context.TempInvoices.Add(tempInvoice);
        }
        tempInvoice.InvoiceBlob = bytes;
        tempInvoice.InvoiceId = request.InvoiceId;
        tempInvoice.SellerPartyId = request.SellerId;
        tempInvoice.BuyerPartyId = request.BuyerId;
        tempInvoice.PaymentMeansId = request.PaymentId;
        await context.SaveChangesAsync();
    }

    public async Task<InvoiceDtoInfo?> GetTempInvoice()
    {
        var tempInvoice = await context.TempInvoices.FirstOrDefaultAsync();
        if (tempInvoice is null)
        {
            return null;
        }
        var json = System.Text.Encoding.UTF8.GetString(tempInvoice.InvoiceBlob);
        var invoiceDto = JsonSerializer.Deserialize<BlazorInvoiceDto>(json);
        if (invoiceDto is null)
        {
            return null;
        }
        return new()
        {
            InvoiceDto = invoiceDto,
            InvoiceId = tempInvoice.InvoiceId ?? 0,
            SellerId = tempInvoice.SellerPartyId,
            BuyerId = tempInvoice.BuyerPartyId,
            PaymentId = tempInvoice.PaymentMeansId
        };
    }
}
