using beinx.db.Models;
using Microsoft.JSInterop;
using pax.XRechnung.NET.AnnotatedDtos;

namespace beinx.db.Services;

public class IndexedDbService : IIndexedDbService
{
    private Task<IJSObjectReference> _moduleTask;
    public IndexedDbService(IJSRuntime jsRuntime)
    {
        _moduleTask = jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/beinx.db/js/beinx-db.js").AsTask();
    }

    public async Task<int> CreatePaymentMeans(PaymentAnnotationDto paymentMeans)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<int>("paymentRepository.createPaymentMeans", paymentMeans);
    }

    public async Task UpdatePaymentMeans(PaymentMeansEntity payment)
    {
        var module = await _moduleTask;
        await module.InvokeVoidAsync("paymentRepository.updatePaymentMeans", payment);
    }

    public async Task DeletePaymentMeans(int id)
    {
        var module = await _moduleTask;
        await module.InvokeVoidAsync("paymentRepository.deletePaymentMeans", id);
    }

    public async Task<List<PaymentMeansEntity>> GetAllPaymentMeans()
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<List<PaymentMeansEntity>>("paymentRepository.getAllPaymentMeans");
    }

    public async Task Clear()
    {
        var module = await _moduleTask;
        await module.InvokeVoidAsync("paymentRepository.clear");
    }
}
