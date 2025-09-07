
using BlazorInvoice.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;

namespace BlazorInvoice.IndexedDb.Services
{
    public interface IIndexedDbService
    {
        Task<int> CreateInvoice(InvoiceDtoInfo invoiceInfo, bool isPaid = false, bool IsImported = false, FinalizeResult? finalizeResult = null);
        Task<int> CreateParty(IPartyBaseDto party, bool isSeller);
        Task<int> CreatePaymentMeans(IPaymentMeansBaseDto paymentMeans);
        Task DeleteInvoice(int id);
        Task DeleteParty(int id);
        Task DeletePaymentMeans(int id);
        Task<List<InvoiceEntity>> GetAllInvoices();
        Task<List<PartyEntity>> GetAllParties();
        Task<List<PaymentEntity>> GetAllPaymentMeans();
        Task<InvoiceEntity?> GetInvoice(int id);
        Task<PartyEntity?> GetParty(int id);
        Task<PaymentEntity?> GetPaymentMeans(int id);
        Task UpdateInvoice(InvoiceEntity entity);
        Task UpdateParty(PartyEntity party);
        Task UpdatePaymentMeans(PaymentEntity entity);
        Task<AppConfigDto?> GetConfig();
        Task SaveConfig(AppConfigDto config);
        Task DownloadBackup();
        Task<string> ExportDb();
        Task UploadBackup(bool replace = false);
        Task ImportDb(string base64, bool replace = false);
    }

    public class IndexedDbService : IIndexedDbService
    {
        private Dictionary<int, PaymentEntity> _payments = new();
        private Dictionary<int, PartyEntity> _parties = new();
        private Dictionary<int, InvoiceEntity> _invoices = new();
        private readonly SemaphoreSlim initSs = new(1, 1);
        private bool isInit;
        private readonly ILogger<IndexedDbService> _logger;

        private Task<IJSObjectReference> _moduleTask;

        public IndexedDbService(IJSRuntime js, ILogger<IndexedDbService> logger)
        {
            _logger = logger;
            _moduleTask = js.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorInvoice.IndexedDb/js/beinx-db.js").AsTask();
        }

        private async Task Init()
        {
            if (isInit)
            {
                return;
            }
            await initSs.WaitAsync();
            if (isInit)
            {
                return;
            }
            try
            {
                var module = await _moduleTask;
                _payments = (await module.InvokeAsync<List<PaymentEntity>>("paymentRepository.getAllPaymentMeans")).ToDictionary(k => k.Id, v => v);
                _parties = (await module.InvokeAsync<List<PartyEntity>>("partyRepository.getAllParties")).ToDictionary(k => k.Id, v => v);
                _invoices = (await module.InvokeAsync<List<InvoiceEntity>>("invoiceRepository.getAllInvoices")).ToDictionary(k => k.Id, v => v);
                isInit = true;
            }
            finally
            {
                initSs.Release();
            }
        }

        public async Task<int> CreateParty(IPartyBaseDto party, bool isSeller)
        {
            await Init();
            var module = await _moduleTask;
            var id = await module.InvokeAsync<int>("partyRepository.createParty", party, isSeller, null);
            var newParty = new PartyEntity { Id = id, Party = party, IsSeller = isSeller };
            _parties[id] = newParty;
            return id;
        }

        public async Task<PartyEntity?> GetParty(int id)
        {
            await Init();
            return _parties.GetValueOrDefault(id);
        }

        public async Task<List<PartyEntity>> GetAllParties()
        {
            await Init();
            return _parties.Values.ToList();
        }

        public async Task UpdateParty(PartyEntity party)
        {
            await Init();
            var module = await _moduleTask;
            await module.InvokeVoidAsync("partyRepository.updateParty", party);
            _parties[party.Id] = party;
        }

        public async Task DeleteParty(int id)
        {
            await Init();
            var module = await _moduleTask;
            await module.InvokeVoidAsync("partyRepository.deleteParty", id);
            _parties.Remove(id);
        }

        public async Task<int> CreatePaymentMeans(IPaymentMeansBaseDto paymentMeans)
        {
            await Init();
            var module = await _moduleTask;
            var id = await module.InvokeAsync<int>("paymentRepository.createPaymentMeans", paymentMeans);
            var newPaymentMeans = new PaymentEntity { Id = id, Payment = paymentMeans };
            _payments[id] = newPaymentMeans;
            return id;
        }

        public async Task<PaymentEntity?> GetPaymentMeans(int id)
        {
            await Init();
            return _payments.GetValueOrDefault(id);
        }

        public async Task<List<PaymentEntity>> GetAllPaymentMeans()
        {
            await Init();
            return _payments.Values.ToList();
        }

        public async Task UpdatePaymentMeans(PaymentEntity entity)
        {
            await Init();
            var module = await _moduleTask;
            await module.InvokeVoidAsync("paymentRepository.updatePaymentMeans", entity);
            _payments[entity.Id] = entity;
        }

        public async Task DeletePaymentMeans(int id)
        {
            await Init();
            var module = await _moduleTask;
            await module.InvokeVoidAsync("paymentRepository.deletePaymentMeans", id);
            _payments.Remove(id);
        }

        public async Task<int> CreateInvoice(InvoiceDtoInfo invoiceInfo, bool isPaid = false, bool IsImported = false, FinalizeResult? finalizeResult = null)
        {
            await Init();
            var module = await _moduleTask;
            var id = await module.InvokeAsync<int>("invoiceRepository.createInvoice", invoiceInfo, invoiceInfo.InvoiceDto.IssueDate.Year, isPaid, IsImported, null);
            InvoiceEntity entity = new()
            {
                Id = id,
                Info = invoiceInfo,
                Year = invoiceInfo.InvoiceDto.IssueDate.Year,
                IsPaid = isPaid,
                IsImported = IsImported,
                FinalizeResult = finalizeResult,
            };
            _invoices[id] = entity;
            return id;
        }

        public async Task<InvoiceEntity?> GetInvoice(int id)
        {
            await Init();
            return _invoices.GetValueOrDefault(id);
        }

        public async Task<List<InvoiceEntity>> GetAllInvoices()
        {
            await Init();
            return _invoices.Values.ToList();
        }

        public async Task UpdateInvoice(InvoiceEntity entity)
        {
            await Init();
            var module = await _moduleTask;
            await module.InvokeVoidAsync("invoiceRepository.updateInvoice", entity);
            _invoices[entity.Id] = entity;
        }

        public async Task DeleteInvoice(int id)
        {
            await Init();
            var module = await _moduleTask;
            await module.InvokeVoidAsync("invoiceRepository.deleteInvoice", id);
            _invoices.Remove(id);
        }

        public async Task<AppConfigDto?> GetConfig()
        {
            var module = await _moduleTask;
            return await module.InvokeAsync<AppConfigDto>("getConfig");
        }

        public async Task SaveConfig(AppConfigDto config)
        {
            var module = await _moduleTask;
            await module.InvokeVoidAsync("saveConfig", config);
        }

        public async Task DownloadBackup()
        {
            var module = await _moduleTask;
            await module.InvokeVoidAsync("downloadBackup");
        }

        public async Task<string> ExportDb()
        {
            var module = await _moduleTask;
            return await module.InvokeAsync<string>("exportDb");
        }

        public async Task UploadBackup(bool replace = false)
        {
            var module = await _moduleTask;
            await module.InvokeVoidAsync("uploadBackup", replace);
        }

        public async Task ImportDb(string base64, bool replace = false)
        {
            var module = await _moduleTask;
            await module.InvokeVoidAsync("importDb", base64, replace);
        }
    }

    public class PartyEntity
    {
        public int Id { get; set; }
        public IPartyBaseDto Party { get; set; } = null!;
        public bool IsSeller { get; set; }
        public bool IsDeleted { get; set; }
        public DocumentReferenceAnnotationDto? Logo { get; set; }
    }

    public class PaymentEntity
    {
        public int Id { get; set; }
        public IPaymentMeansBaseDto Payment { get; set; } = null!;
        public bool IsDeleted { get; set; }
    }

    public class InvoiceEntity
    {
        public int Id { get; set; }
        public InvoiceDtoInfo Info { get; set; } = null!;
        public int Year { get; set; }
        public bool IsPaid { get; set; }
        public bool IsImported { get; set; }
        public bool IsDeleted { get; set; }
        public FinalizeResult? FinalizeResult { get; set; }
    }
}
