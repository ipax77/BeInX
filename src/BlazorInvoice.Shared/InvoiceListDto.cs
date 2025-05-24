namespace BlazorInvoice.Shared;

public class InvoiceListDto
{
    public int InvoiceId { get; set; }
    public string Id { get; init; } = string.Empty;
    public DateTime IssueDate { get; init; }
    public string BuyerEmail { get; init; } = string.Empty;
    public bool IsFinalized { get; init; }
    public bool IsPaid { get; init; }
}
