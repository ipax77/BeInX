using pax.XRechnung.NET.AnnotatedDtos;

namespace beinx.shared;

public class PaymentMeansEntity
{
    public int? Id { get; set; }
    public PaymentAnnotationDto Payment { get; set; } = null!;
}