using System.Text.Json;
using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlazorInvoice.Db.Repository;

public class WorkListRepository(InvoiceContext context) : IWorkListRepository
{
    public async Task<WorkEntrySnapShot> GetWorkEntries()
    {
        var entries = await context.WorkEntries
            .OrderBy(o => o.StartTime)
            .ToListAsync();
        return new()
        {
            EntriesByParty = entries
            .GroupBy(g => g.InvoicePartyId)
            .ToDictionary(p => p.Key, v => v.Select(s => MapWorkEntry(s)).ToList())
        };
    }

    public async Task<List<WorkEntryDto>> GetBilledWorkEntries(int partyId, int year)
    {
        var lines = await context.InvoiceLines
            .Where(x => x.Invoice!.BuyerParty!.InvoicePartyId == partyId
                && x.Invoice.IssueDate.Year == year)
            .OrderBy(o => o.StartDate)
            .Select(s => new { s, s.Invoice!.IssueDate })
            .ToListAsync();

        return lines.Select(s => MapWorkEntry(s.s, s.IssueDate, partyId)).ToList();
    }

    public async Task SaveWorkEntries(WorkEntrySnapShot snapshot)
    {
        foreach (var partyId in snapshot.EntriesByParty.Keys)
        {
            var existingEntries = await context.WorkEntries
                .Where(x => x.InvoicePartyId == partyId)
                .ToListAsync();
            context.WorkEntries.RemoveRange(existingEntries);
        }

        var entries = snapshot.EntriesByParty.SelectMany(s => s.Value).Select(t => MapWorkEntry(t));
        context.WorkEntries.AddRange(entries);
        await context.SaveChangesAsync();
        await DeleteTempWorkEntries();
    }

    private async Task DeleteTempWorkEntries()
    {
        var entry = await context.TempWorkEntries
            .OrderBy(o => o.TempWorkEntryId)
            .FirstOrDefaultAsync();
        if (entry is not null)
        {
            context.TempWorkEntries.Remove(entry);
            await context.SaveChangesAsync();
        }
    }

    public async Task SaveTempWorkEntriesAsync(List<WorkEntryDto> entries)
    {
        var temp = await context.TempWorkEntries
            .OrderBy(o => o.TempWorkEntryId)
            .FirstOrDefaultAsync();

        var blob = SerializeWorkEntries(entries);

        if (temp == null)
        {
            temp = new TempWorkEntry
            {
                WorkEntriesBlob = blob,
                LastModified = DateTime.UtcNow
            };
            context.TempWorkEntries.Add(temp);
        }
        else
        {
            temp.WorkEntriesBlob = blob;
            temp.LastModified = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<WorkEntryDto>> LoadTempWorkEntriesAsync()
    {
        var temp = await context.TempWorkEntries
            .OrderBy(o => o.TempWorkEntryId)
            .FirstOrDefaultAsync();

        if (temp == null || temp.WorkEntriesBlob.Length == 0)
            return [];

        return DeserializeWorkEntries(temp.WorkEntriesBlob);
    }

    public async Task<bool> HasTempWorkEntries()
    {
        return await context.TempWorkEntries
            .AnyAsync();
    }

    private static WorkEntryDto MapWorkEntry(WorkEntry workEntry)
    {
        return new()
        {
            EntryGuid = workEntry.EntryGuid,
            Date = new DateOnly(workEntry.StartTime.Year, workEntry.StartTime.Month, workEntry.StartTime.Day),
            Job = workEntry.Job,
            StartTime = new TimeOnly(workEntry.StartTime.Hour, workEntry.StartTime.Minute, workEntry.StartTime.Second),
            EndTime = new TimeOnly(workEntry.EndTime.Hour, workEntry.EndTime.Minute, workEntry.EndTime.Second),
            Billed = workEntry.Billed,
            HourlyRate = (double)workEntry.HourlyRate,
            PartyId = workEntry.InvoicePartyId,
        };
    }

    private static WorkEntry MapWorkEntry(WorkEntryDto workEntry)
    {

        return new()
        {
            EntryGuid = workEntry.EntryGuid,
            Job = workEntry.Job,
            StartTime = workEntry.Date.ToDateTime(workEntry.StartTime),
            EndTime = workEntry.Date.ToDateTime(workEntry.EndTime),
            Billed = workEntry.Billed,
            HourlyRate = (decimal)workEntry.HourlyRate,
            InvoicePartyId = workEntry.PartyId,
        };
    }

    private static WorkEntryDto MapWorkEntry(InvoiceLine invoiceLine, DateTime issueDate, int partyId)
    {
        DateTime startDate = invoiceLine.StartDate ?? issueDate;
        DateTime endDate = invoiceLine.EndDate ?? issueDate.AddHours(invoiceLine.Quantity);
        return new()
        {
            Job = invoiceLine.Name,
            Date = DateOnly.FromDateTime(startDate),
            StartTime = TimeOnly.FromDateTime(startDate),
            EndTime = TimeOnly.FromDateTime(endDate),
            Billed = true,
            HourlyRate = invoiceLine.UnitPrice,
            PartyId = partyId,
        };
    }

    private static InvoiceLine MapWorkEntry(WorkEntryDto workEntry, DateTime issueDate)
    {
        DateTime startDate = workEntry.Date.ToDateTime(workEntry.StartTime);
        DateTime endDate = workEntry.Date.ToDateTime(workEntry.EndTime);
        return new()
        {
            Name = workEntry.Job,
            StartDate = startDate,
            EndDate = endDate,
            QuantityCode = "HUR",
            Quantity = (endDate - startDate).TotalHours,
            UnitPrice = workEntry.HourlyRate,
        };
    }

    public static byte[] SerializeWorkEntries(List<WorkEntryDto> entries)
    {
        return JsonSerializer.SerializeToUtf8Bytes(entries);
    }

    public static List<WorkEntryDto> DeserializeWorkEntries(byte[] blob)
    {
        return JsonSerializer.Deserialize<List<WorkEntryDto>>(blob) ?? [];
    }

}