using beinx.shared;
using beinx.shared.Interfaces;
using pax.XRechnung.NET.AnnotatedDtos;

namespace beinx.db.Services;

public class PaymentsRepository(IndexedDbInterop interop) : IPaymentsRepository
{
    private readonly IndexedDbInterop _interop = interop;

    public Task<int> CreateAsync(PaymentAnnotationDto dto)
        => _interop.CallAsync<int>("paymentRepository.createPaymentMeans", dto);

    public async Task UpdateAsync(PaymentMeansEntity payment)
        => await _interop.CallVoidAsync("paymentRepository.updatePaymentMeans", payment.Id ?? 0, payment.Payment);

    public async Task DeleteAsync(int id)
        => await _interop.CallVoidAsync("paymentRepository.deletePaymentMeans", id);

    public Task<List<PaymentMeansEntity>> GetAllAsync()
        => _interop.CallAsync<List<PaymentMeansEntity>>("paymentRepository.getAllPaymentMeans");

    public async Task Clear()
        => await _interop.CallVoidAsync("paymentRepository.clea");
}
