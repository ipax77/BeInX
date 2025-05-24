namespace BlazorInvoice.Shared;

public class PartyListDto
{
    public int PartyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}