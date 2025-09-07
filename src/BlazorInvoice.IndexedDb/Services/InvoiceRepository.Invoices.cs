
using BlazorInvoice.Shared;

namespace BlazorInvoice.IndexedDb.Services
{
    public partial class InvoiceRepository
    {
        public async Task<int> CreateInvoice(BlazorInvoiceDto invoiceDto, int sellerId, int buyerId, int paymentId, bool isImported = false, CancellationToken token = default)
        {
            var invoiceInfo = new InvoiceDtoInfo
            {
                InvoiceDto = invoiceDto,
                InvoiceId = 0,
                SellerId = sellerId,
                BuyerId = buyerId,
                PaymentId = paymentId
            };
            return await _indexedDbService.CreateInvoice(invoiceInfo, false, isImported, null);
        }

        public async Task DeleteInvoice(int invoiceId, CancellationToken token = default)
        {
            await _indexedDbService.DeleteInvoice(invoiceId);
        }

        public async Task<InvoiceDtoInfo?> GetInvoice(int invoiceId, CancellationToken token = default)
        {
            var invoice = await _indexedDbService.GetInvoice(invoiceId);
            return invoice?.Info;
        }

        public async Task<List<InvoiceListDto>> GetInvoices(InvoiceListRequest request, CancellationToken token = default)
        {
            var invoices = await _indexedDbService.GetAllInvoices();
            var filtered = invoices.AsEnumerable();

            if (request.Unpaid)
            {
                filtered = filtered.Where(x => !x.IsPaid);
            }

            if (!string.IsNullOrEmpty(request.Filter))
            {
                var filter = request.Filter.ToLowerInvariant();
                filtered = filtered.Where(i =>
                    i.Info.InvoiceDto.BuyerParty.Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
            }

            var sorted = ApplyInvoiceSorting(filtered, request.TableOrders);
            var result = sorted.Skip(request.Skip).Take(request.Take).Select(ToInvoiceListDto).ToList();

            return result;
        }

        public async Task<int> GetInvoicesCount(InvoiceListRequest request, CancellationToken token = default)
        {
            var invoices = await _indexedDbService.GetAllInvoices();
            var filtered = invoices.AsEnumerable();

            if (request.Unpaid)
            {
                filtered = filtered.Where(x => !x.IsPaid);
            }

            if (!string.IsNullOrEmpty(request.Filter))
            {
                var filter = request.Filter.ToLowerInvariant();
                filtered = filtered.Where(i =>
                    i.Info.InvoiceDto.BuyerParty.Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
            }

            return filtered.Count();
        }

        public async Task UpdateInvoice(int invoiceId, BlazorInvoiceDto invoiceDto, CancellationToken token = default)
        {
            var invoice = await _indexedDbService.GetInvoice(invoiceId);
            if (invoice != null)
            {
                invoice.Info = new InvoiceDtoInfo()
                {
                    InvoiceDto = invoiceDto,
                    InvoiceId = invoice.Info.InvoiceId,
                    SellerId = invoice.Info.SellerId,
                    BuyerId = invoice.Info.BuyerId,
                    PaymentId = invoice.Info.PaymentId,
                };
                await _indexedDbService.UpdateInvoice(invoice);
            }
        }

        public async Task SetIsPaid(int invoiceId, bool isPaid, CancellationToken token = default)
        {
            var invoice = await _indexedDbService.GetInvoice(invoiceId);
            if (invoice != null)
            {
                invoice.IsPaid = isPaid;
                await _indexedDbService.UpdateInvoice(invoice);
            }
        }

        private InvoiceListDto ToInvoiceListDto(InvoiceEntity entity)
        {
            return new InvoiceListDto
            {
                InvoiceId = entity.Id,
                Id = entity.Info.InvoiceDto.Id,
                BuyerEmail = entity.Info.InvoiceDto.BuyerParty.Email,
                IssueDate = entity.Info.InvoiceDto.IssueDate,
                IsPaid = entity.IsPaid
            };
        }

        private static IEnumerable<InvoiceEntity> ApplyInvoiceSorting(IEnumerable<InvoiceEntity> invoices, List<TableOrder> orders)
        {
            if (!orders.Any())
            {
                return invoices.OrderByDescending(i => i.Info.InvoiceDto.IssueDate);
            }

            IOrderedEnumerable<InvoiceEntity>? orderedInvoices = null;

            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                var propertyName = order.PropertyName.ToLowerInvariant();

                if (orderedInvoices is null)
                {
                    orderedInvoices = propertyName switch
                    {
                        "id" => order.Ascending ? invoices.OrderBy(p => p.Id) : invoices.OrderByDescending(p => p.Id),
                        "buyeremail" => order.Ascending ? invoices.OrderBy(p => p.Info.InvoiceDto.BuyerParty.Email) : invoices.OrderByDescending(p => p.Info.InvoiceDto.BuyerParty.Email),
                        "issuedate" => order.Ascending ? invoices.OrderBy(p => p.Info.InvoiceDto.IssueDate) : invoices.OrderByDescending(p => p.Info.InvoiceDto.IssueDate),
                        _ => invoices.OrderByDescending(p => p.Info.InvoiceDto.IssueDate)
                    };
                }
                else
                {
                    orderedInvoices = propertyName switch
                    {
                        "id" => order.Ascending ? orderedInvoices.ThenBy(p => p.Id) : orderedInvoices.ThenByDescending(p => p.Id),
                        "buyeremail" => order.Ascending ? orderedInvoices.ThenBy(p => p.Info.InvoiceDto.BuyerParty.Email) : orderedInvoices.ThenByDescending(p => p.Info.InvoiceDto.BuyerParty.Email),
                        "issuedate" => order.Ascending ? orderedInvoices.ThenBy(p => p.Info.InvoiceDto.IssueDate) : orderedInvoices.ThenByDescending(p => p.Info.InvoiceDto.IssueDate),
                        _ => orderedInvoices.ThenByDescending(p => p.Info.InvoiceDto.IssueDate)
                    };
                }
            }

            return orderedInvoices ?? invoices.OrderByDescending(i => i.Info.InvoiceDto.IssueDate);
        }
    }
}
