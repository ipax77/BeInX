using beinx.shared;
using beinx.shared.Interfaces;

namespace beinx.db.Services;

public class InvoiceRepository(IIndexedDbInterop _interop)
    : IInvoiceRepository, IDraftRepository<InvoiceDtoInfo>
{
    public Task<int> CreateAsync(InvoiceDtoInfo dto, bool isImported = false)
        => _interop.CallAsync<int>("invoiceRepository.createInvoice", dto, isImported);

    public Task UpdateAsync(int id, InvoiceDtoInfo updatedInfo)
        => _interop.CallVoidAsync("invoiceRepository.updateInvoice", id, updatedInfo);

    public Task DeleteAsync(int id)
        => _interop.CallVoidAsync("invoiceRepository.deleteInvoice", id);

    public Task<List<InvoiceEntity>> GetAllAsync()
        => _interop.CallAsync<List<InvoiceEntity>>("invoiceRepository.getAllInvoices");

    public Task<InvoiceEntity?> GetByIdAsync(int id)
        => _interop.CallAsync<InvoiceEntity?>("invoiceRepository.getById", id);

    public Task<List<InvoiceEntity>> FindByYearAsync(int year)
        => _interop.CallAsync<List<InvoiceEntity>>("invoiceRepository.findByYear", year);

    public Task MarkAsPaidAsync(int id)
        => _interop.CallVoidAsync("invoiceRepository.markAsPaid", id);

    public Task FinalizeAsync(int id, FinalizeResult finalizeResult)
        => _interop.CallVoidAsync("invoiceRepository.finalizeInvoice", id, finalizeResult);

    public Task<List<InvoiceListItem>> GetListAsync(int limit = 50, int offset = 0)
        => _interop.CallAsync<List<InvoiceListItem>>("invoiceRepository.getInvoiceList", limit, offset);

    public Task<int> GetCountAsync()
        => _interop.CallAsync<int>("invoiceRepository.getInvoiceCount");

    public Task<int> GetCountByYearAsync(int year)
        => _interop.CallAsync<int>("invoiceRepository.getInvoiceCountByYear", year);

    public Task<int> GetCountByPaidStatusAsync(bool isPaid)
        => _interop.CallAsync<int>("invoiceRepository.getInvoiceCountByPaidStatus", isPaid);

    public Task ClearAsync()
        => _interop.CallVoidAsync("invoiceRepository.clear");

    // ---- Draft management ----
    public async Task<DraftState<InvoiceDtoInfo>?> LoadDraftAsync()
    {
        var draft = await _interop.CallAsync<Draft<InvoiceDtoInfo>>("invoiceRepository.loadTempInvoice");
        if (draft == null)
            return null;
        return new DraftState<InvoiceDtoInfo>
        {
            EntityId = draft.EntityId,
            Data = draft.Data
        };
    }

    public Task SaveDraftAsync(DraftState<InvoiceDtoInfo> draft)
        => _interop.CallVoidAsync("invoiceRepository.saveTempInvoice", draft.Data, draft.EntityId ?? default);

    public Task ClearDraftAsync(int? entityId)
        => _interop.CallVoidAsync("invoiceRepository.clearTempInvoice", entityId ?? default);
}
