namespace BlazorInvoice.Shared.Interfaces;

public interface IWorkListRepository
{
    Task<WorkEntrySnapShot> GetWorkEntries();
    Task SaveWorkEntries(WorkEntrySnapShot snapshot);
    Task SaveTempWorkEntriesAsync(List<WorkEntryDto> entries);
    Task<List<WorkEntryDto>> LoadTempWorkEntriesAsync();
    Task<bool> HasTempWorkEntries();
    Task<List<WorkEntryDto>> GetBilledWorkEntries(int partyId, int year);
}