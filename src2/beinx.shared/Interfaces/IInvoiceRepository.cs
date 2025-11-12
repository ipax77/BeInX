namespace beinx.shared.Interfaces;

public interface IInvoiceRepository
{
    Task<int> CreateAsync(InvoiceDtoInfo dto, bool isImported = false);
    Task UpdateAsync(int id, InvoiceDtoInfo updatedInfo);
    Task DeleteAsync(int id);
    Task<List<InvoiceEntity>> GetAllAsync();
    Task<InvoiceEntity?> GetByIdAsync(int id);
    Task<List<InvoiceEntity>> FindByYearAsync(int year);
    Task SetPaidAsync(int id, bool isPaid);
    Task FinalizeAsync(int id, FinalizeResult finalizeResult);
    Task<List<InvoiceListItem>> GetListAsync(int limit = 50, int offset = 0);
    Task<int> GetCountAsync();
    Task<int> GetCountByYearAsync(int year);
    Task<int> GetCountByPaidStatusAsync(bool isPaid);
    Task ClearAsync();
}