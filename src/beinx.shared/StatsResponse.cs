namespace beinx.shared;

public class StatsResponse
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public List<StatsStepResponse> Steps { get; init; } = [];
    public int TotalInvoices { get; init; }
    public List<InvoiceListItem> UnpaidInvoices { get; init; } = [];
}

public class StatsStepResponse
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public double TotalAmountWithVat { get; init; }
}