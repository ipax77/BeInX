namespace beinx.shared;

public class StatsResponse
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public List<StatsStepResponse> Steps { get; init; } = [];
    public decimal UnpaidAmount { get; init; }
    public int TotalInvoices { get; init; }
    public int PaidInvoices { get; init; }
}

public class StatsStepResponse
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public decimal TotalAmountWithoutVat { get; init; }
}