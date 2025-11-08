using pax.XRechnung.NET.AnnotatedDtos;

namespace beinx.db.Models;

public class PaymentMeansEntity
{
    public int? Id { get; set; }
    public PaymentAnnotationDto Payment { get; set; } = null!;
}
