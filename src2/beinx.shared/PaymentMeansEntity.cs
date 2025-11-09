using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;

namespace beinx.shared;

public class PaymentMeansEntity
{
    public int? Id { get; set; }
    public PaymentAnnotationDto Payment { get; set; } = null!;
}

public class Draft<T>
{
    public string Id { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public T Data { get; set; } = default!;
    public DateTime UpdatedAt { get; set; }
}

public class PartyEntity
{
    public int? Id { get; set; }
    public IPartyBaseDto Party { get; set; } = null!;
}