namespace BlazorInvoice.Shared;

public class PaymentListDto
{
    public int PlaymentMeansId { get; set; }
    public string Iban { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}