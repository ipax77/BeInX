using System.Text.Json;
using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlazorInvoice.Db.Repository;

public class WorkListRepository(InvoiceContext context) : IWorkListRepository
{
    public async Task<List<WorkEntryDto>> GetWorkEntires(int partyId, bool billed)
    {
        var entries = await context.WorkEntries
            .Where(x => x.InvoicePartyId == partyId && x.Billed == billed)
            .OrderBy(o => o.Date)
            .ToListAsync();
        return entries.Select(s => MapWorkEntry(s)).ToList();
    }

    public async Task SaveWorkEntries(List<WorkEntryDto> workEntries, int partyId)
    {
        var entires = workEntries.Select(s => MapWorkEntry(s, partyId));
        context.WorkEntries.AddRange(entires);
        await context.SaveChangesAsync();
        await DeleteTempWorkEntries(partyId);
    }

    private async Task DeleteTempWorkEntries(int partyId)
    {
        var entry = await context.TempWorkEntries
            .OrderBy(o => o.TempWorkEntryId)
            .Where(x => x.InvoicePartyId == partyId)
            .FirstOrDefaultAsync();
        if (entry is not null)
        {
            context.TempWorkEntries.Remove(entry);
            await context.SaveChangesAsync();
        }
    }

    public async Task SaveTempWorkEntriesAsync(List<WorkEntryDto> entries, int partyId)
    {
        var temp = await context.TempWorkEntries
            .FirstOrDefaultAsync(t => t.InvoicePartyId == partyId);

        var blob = SerializeWorkEntries(entries);

        if (temp == null)
        {
            temp = new TempWorkEntry
            {
                InvoicePartyId = partyId,
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

    public async Task<List<WorkEntryDto>> LoadTempWorkEntriesAsync(int partyId)
    {
        var temp = await context.TempWorkEntries
            .FirstOrDefaultAsync(t => t.InvoicePartyId == partyId);

        if (temp == null || temp.WorkEntriesBlob.Length == 0)
            return [];

        return DeserializeWorkEntries(temp.WorkEntriesBlob);
    }

    public async Task<bool> HasTempWorkEntries(int partyId)
    {
        return await context.TempWorkEntries
            .Where(x => x.InvoicePartyId == partyId)
            .AnyAsync();
    }

    private static WorkEntryDto MapWorkEntry(WorkEntry workEntry)
    {
        return new()
        {
            EntryGuid = workEntry.EntryGuid,
            Date = workEntry.Date,
            Job = workEntry.Job,
            StartTime = workEntry.StartTime,
            EndTime = workEntry.EndTime,
            Billed = workEntry.Billed,
            HourlyRate = workEntry.HourlyRate,
        };
    }

    private static WorkEntry MapWorkEntry(WorkEntryDto workEntry, int partyId)
    {
        return new()
        {
            EntryGuid = workEntry.EntryGuid,
            Date = workEntry.Date,
            Job = workEntry.Job,
            StartTime = workEntry.StartTime,
            EndTime = workEntry.EndTime,
            Billed = workEntry.Billed,
            HourlyRate = workEntry.HourlyRate,
            InvoicePartyId = partyId,
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