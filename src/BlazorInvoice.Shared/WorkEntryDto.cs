namespace BlazorInvoice.Shared;

public record WorkEntryDto
{
    public Guid EntryGuid { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; }
    public string Job { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Billed { get; set; }
    public decimal HourlyRate { get; set; }
}