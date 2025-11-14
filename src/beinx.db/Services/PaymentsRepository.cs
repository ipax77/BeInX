using beinx.shared;
using beinx.shared.Interfaces;
using pax.XRechnung.NET.AnnotatedDtos;

namespace beinx.db.Services;

public class PaymentsRepository(IIndexedDbInterop _interop) : IPaymentsRepository, IDraftRepository<PaymentAnnotationDto>
{

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

    public async Task<DraftState<PaymentAnnotationDto>?> LoadDraftAsync()
    {
        var draft = await _interop.CallAsync<Draft<PaymentAnnotationDto>>("paymentRepository.loadTempPayment");
        if (draft == null)
            return null;
        return new DraftState<PaymentAnnotationDto>()
        {
            EntityId = draft.EntityId,
            Data = draft.Data
        };
    }

    public async Task SaveDraftAsync(DraftState<PaymentAnnotationDto> draft)
    {
        await _interop.CallVoidAsync("paymentRepository.saveTempPayment", draft.Data, draft.EntityId ?? default);
    }

    public async Task ClearDraftAsync(int? entityId)
    {
        await _interop.CallVoidAsync("paymentRepository.clearTempPayment", entityId ?? default);
    }
}
