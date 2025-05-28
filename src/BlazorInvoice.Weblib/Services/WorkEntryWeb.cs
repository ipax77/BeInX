using BlazorInvoice.Shared;
using Microsoft.AspNetCore.Components;

namespace BlazorInvoice.Weblib.Services;

public record WorkEntryWeb : WorkEntryDto
{
    public WorkEntryWeb(WorkEntryDto entryDto, int columnCount)
    {
        EntryGuid = entryDto.EntryGuid;
        Date = entryDto.Date;
        Job = entryDto.Job;
        StartTime = entryDto.StartTime;
        EndTime = entryDto.EndTime;
        HourlyRate = entryDto.HourlyRate;
        ElementReferences = new ElementReference[columnCount];
    }
    public ElementReference[] ElementReferences { get; set; }
    public int Row { get; set; } = -1;

    public WorkEntryDto GetDto()
    {
        return new WorkEntryDto
        {
            EntryGuid = EntryGuid,
            Date = Date,
            Job = Job,
            StartTime = StartTime,
            EndTime = EndTime,
            HourlyRate = HourlyRate
        };
    }

    public bool IsEmpty() =>
        string.IsNullOrEmpty(Job) &&
        StartTime == default &&
        EndTime == default &&
        HourlyRate == default;
}
