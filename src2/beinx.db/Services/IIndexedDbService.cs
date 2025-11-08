using beinx.db.Models;
using pax.XRechnung.NET.AnnotatedDtos;

namespace beinx.db.Services;

public interface IIndexedDbService
{
    Task<int> CreatePaymentMeans(PaymentAnnotationDto paymentMeans);
    Task UpdatePaymentMeans(PaymentMeansEntity payment);
    Task DeletePaymentMeans(int id);
    Task<List<PaymentMeansEntity>> GetAllPaymentMeans();
    Task Clear();
}
