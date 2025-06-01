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
    public int PartyId { get; set; }
}

public record WorkEntrySnapShot
{
    public Dictionary<int, List<WorkEntryDto>> EntriesByParty { get; set; } = [];
}

public enum WorkEntryChangeType
{
    Create,
    Edit,
    Delete
}

public record WorkEntryDelta
{
    public WorkEntryChangeType ChangeType { get; init; }
    public WorkEntryDto? Before { get; init; }
    public WorkEntryDto? After { get; init; }
    public int PartyId { get; init; }
}