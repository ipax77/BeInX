using pax.XRechnung.NET.AnnotatedDtos;

namespace beinx.shared.Interfaces;

public interface IPaymentsRepository
{
    Task Clear();
    Task<int> CreateAsync(PaymentAnnotationDto dto);
    Task DeleteAsync(int id);
    Task<List<PaymentMeansEntity>> GetAllAsync();
    Task UpdateAsync(PaymentMeansEntity payment);
}
