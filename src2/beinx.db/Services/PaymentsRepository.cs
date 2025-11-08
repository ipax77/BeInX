using beinx.shared;
using beinx.shared.Interfaces;
using pax.XRechnung.NET.AnnotatedDtos;

namespace beinx.db.Services;

public class PaymentsRepository(IndexedDbInterop interop) : IPaymentsRepository
{
    private readonly IndexedDbInterop _interop = interop;

    public Task<int> CreateAsync(PaymentAnnotationDto dto)
        => _interop.CallAsync<int>("createPaymentMeans", dto);

    public async Task UpdateAsync(PaymentMeansEntity payment)
        => await _interop.CallVoidAsync("updatePaymentMeans", payment);

    public async Task DeleteAsync(int id)
        => await _interop.CallVoidAsync("deletePaymentMeans", id);

    public Task<List<PaymentMeansEntity>> GetAllAsync()
        => _interop.CallAsync<List<PaymentMeansEntity>>("getAllPaymentMeans");

    public async Task Clear()
        => await _interop.CallVoidAsync("clearPaymentMeans");
}
