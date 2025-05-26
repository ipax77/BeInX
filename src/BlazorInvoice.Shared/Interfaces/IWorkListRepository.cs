namespace BlazorInvoice.Shared.Interfaces;

public interface IWorkListRepository
{
    Task<List<WorkEntryDto>> GetWorkEntires(int partyId, bool billed);
    Task SaveWorkEntries(List<WorkEntryDto> workEntries, int partyId);
    Task SaveTempWorkEntriesAsync(List<WorkEntryDto> entries, int partyId);
    Task<List<WorkEntryDto>> LoadTempWorkEntriesAsync(int partyId);
    Task<bool> HasTempWorkEntries(int partyId);
}