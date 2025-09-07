
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace BlazorInvoice.IndexedDb.Services;

public partial class IndexedDbService(IJSRuntime js, ILogger<IndexedDbService> logger)
{
    private Dictionary<int, PaymentEntity> _payments = [];
    private Dictionary<int, PartyEntity> _parties = [];
    private Dictionary<int, InvoiceEntity> _invoices = [];
    private readonly SemaphoreSlim initSs = new(1, 1);
    private bool isInit;

    private Task<IJSObjectReference> moduleTask = js.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorInvoice.IndexedDb/js/beinx-db.js").AsTask();

    private async Task Init()
    {
        if (isInit)
        {
            return;
        }
        await initSs.WaitAsync();
        if (isInit)
        {
            return;
        }
        try
        {
            var module = await moduleTask;
            _payments = (await module.InvokeAsync<List<PaymentEntity>>("paymentRepository.getAllPayments")).ToDictionary(k => k.Id, v => v);
            _parties = (await module.InvokeAsync<List<PartyEntity>>("partyRepository.getAllParties")).ToDictionary(k => k.Id, v => v);
            _invoices = (await module.InvokeAsync<List<InvoiceEntity>>("invoiceRepository.getAllInvoices")).ToDictionary(k => k.Id, v => v);
            isInit = true;
        }
        finally
        {
            initSs.Release();
        }
    }
}

internal sealed class PaymentEntity
{
    public int Id { get; set; }
}

internal sealed class PartyEntity
{
    public int Id { get; set; }
}

internal sealed class InvoiceEntity
{
    public int Id { get; set; }
}