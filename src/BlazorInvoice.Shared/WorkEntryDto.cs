namespace BlazorInvoice.Shared;

public record WorkEntryDto
{
    public Guid EntryGuid { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
    public string Job { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool Billed { get; set; }
    public double HourlyRate { get; set; }
}