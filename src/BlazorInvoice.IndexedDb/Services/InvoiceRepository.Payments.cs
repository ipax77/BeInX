
using BlazorInvoice.Shared;
using pax.XRechnung.NET.BaseDtos;
using pax.XRechnung.NET.AnnotatedDtos;

namespace BlazorInvoice.IndexedDb.Services
{
    public partial class InvoiceRepository
    {
        // Payments
        public async Task<int> CreatePaymentMeans(IPaymentMeansBaseDto paymentMeans, CancellationToken token = default)
        {
            return await _indexedDbService.CreatePaymentMeans(paymentMeans);
        }

        public async Task DeletePaymentMeans(int paymentMeansId, CancellationToken token = default)
        {
            await _indexedDbService.DeletePaymentMeans(paymentMeansId);
        }

        public async Task<PaymentAnnotationDto> GetPaymentMeans(int paymentMeansId, CancellationToken token = default)
        {
            var entity = await _indexedDbService.GetPaymentMeans(paymentMeansId);
            return ToAnnotationDto(entity);
        }

        public async Task<List<PaymentListDto>> GetPayments(InvoiceListRequest request, CancellationToken token = default)
        {
            var payments = await _indexedDbService.GetAllPaymentMeans();
            var filtered = payments.AsEnumerable();

            if (!string.IsNullOrEmpty(request.Filter))
            {
                var filter = request.Filter.ToLowerInvariant();
                filtered = filtered.Where(p =>
                    p.Payment.Name.ToLowerInvariant().Contains(filter) ||
                    p.Payment.Iban.ToLowerInvariant().Contains(filter));
            }

            var sorted = ApplyPaymentSorting(filtered, request.TableOrders);
            var result = sorted.Skip(request.Skip).Take(request.Take).Select(ToListDto).ToList();

            return result;
        }

        public async Task<int> GetPaymentsCount(InvoiceListRequest request, CancellationToken token = default)
        {
            var payments = await _indexedDbService.GetAllPaymentMeans();
            var filtered = payments.AsEnumerable();

            if (!string.IsNullOrEmpty(request.Filter))
            {
                var filter = request.Filter.ToLowerInvariant();
                filtered = filtered.Where(p =>
                    p.Payment.Name.ToLowerInvariant().Contains(filter) ||
                    p.Payment.Iban.ToLowerInvariant().Contains(filter));
            }

            return filtered.Count();
        }

        public async Task UpdatePaymentMeans(int paymentMeansId, IPaymentMeansBaseDto paymentMeans, CancellationToken token = default)
        {
            var entity = await _indexedDbService.GetPaymentMeans(paymentMeansId);
            if (entity != null)
            {
                entity.Payment = paymentMeans;
                await _indexedDbService.UpdatePaymentMeans(entity);
            }
        }
        // End Payments

        // Mappers
        private PaymentAnnotationDto ToAnnotationDto(PaymentEntity? entity)
        {
            if (entity == null) return new();
            return new PaymentAnnotationDto
            {
                Name = entity.Payment.Name,
                Iban = entity.Payment.Iban,
                Bic = entity.Payment.Bic,
                PaymentMeansTypeCode = entity.Payment.PaymentMeansTypeCode
            };
        }

        private PaymentListDto ToListDto(PaymentEntity? entity)
        {
            if (entity == null) return new();
            return new PaymentListDto
            {
                Name = entity.Payment.Name,
                Iban = entity.Payment.Iban
            };
        }

        private static IEnumerable<PaymentEntity> ApplyPaymentSorting(IEnumerable<PaymentEntity> payments, List<TableOrder> orders)
        {
            if (orders.Count == 0)
            {
                return payments.OrderBy(p => p.Payment.Name);
            }

            IOrderedEnumerable<PaymentEntity>? orderedPayments = null;

            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                var propertyName = order.PropertyName.ToLowerInvariant();

                if (orderedPayments is null)
                {
                    orderedPayments = propertyName switch
                    {
                        "name" => order.Ascending ? payments.OrderBy(p => p.Payment.Name) : payments.OrderByDescending(p => p.Payment.Name),
                        "iban" => order.Ascending ? payments.OrderBy(p => p.Payment.Iban) : payments.OrderByDescending(p => p.Payment.Iban),
                        _ => payments.OrderBy(p => p.Payment.Name)
                    };
                }
                else
                {
                    orderedPayments = propertyName switch
                    {
                        "name" => order.Ascending ? orderedPayments.ThenBy(p => p.Payment.Name) : orderedPayments.ThenByDescending(p => p.Payment.Name),
                        "iban" => order.Ascending ? orderedPayments.ThenBy(p => p.Payment.Iban) : orderedPayments.ThenByDescending(p => p.Payment.Iban),
                        _ => orderedPayments.ThenBy(p => p.Payment.Name)
                    };
                }
            }

            return orderedPayments ?? payments.OrderBy(p => p.Payment.Name);
        }
    }
}
