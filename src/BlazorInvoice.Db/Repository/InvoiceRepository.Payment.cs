using BlazorInvoice.Shared;
using Microsoft.EntityFrameworkCore;
using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;
using System.Linq.Expressions;

namespace BlazorInvoice.Db.Repository;

public partial class InvoiceRepository
{
    public async Task<int> GetPaymentsCount(InvoiceListRequest request, CancellationToken token = default)
    {
        var query = GetPaymentListQueryable(request);
        return await query.CountAsync(token);
    }

    public async Task<List<PaymentListDto>> GetPayments(InvoiceListRequest request, CancellationToken token = default)
    {
        var query = GetPaymentListQueryable(request);
        query = GetPaymentSortedQueryable(query, request.TableOrders);
        return await query
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(token);
    }

    private static IOrderedQueryable<PaymentListDto> GetPaymentSortedQueryable(IQueryable<PaymentListDto> query, List<TableOrder> orders)
    {
        if (orders.Count == 0)
        {
            return query.OrderBy(i => i.Name);
        }

        IOrderedQueryable<PaymentListDto>? orderedQuery = null;

        for (int i = 0; i < orders.Count; i++)
        {
            var order = orders[i];
            orderedQuery = ApplyPaymentsOrdering(query, orderedQuery, order.PropertyName.ToLowerInvariant(), order.Ascending, i == 0);
        }
        return orderedQuery ?? query.OrderBy(i => i.Name);
    }

    private static IOrderedQueryable<PaymentListDto> ApplyPaymentsOrdering(
        IQueryable<PaymentListDto> baseQuery,
        IOrderedQueryable<PaymentListDto>? currentQuery,
        string property,
        bool ascending,
        bool isFirstOrder)
    {
        Expression<Func<PaymentListDto, object>> keySelector = property switch
        {
            "name" => x => x.Name,
            "iban" => x => x.Iban,
            _ => x => x.Name
        };

        if (isFirstOrder)
        {
            return ascending
                ? baseQuery.OrderBy(keySelector)
                : baseQuery.OrderByDescending(keySelector);
        }
        else
        {
            return ascending
                ? currentQuery!.ThenBy(keySelector)
                : currentQuery!.ThenByDescending(keySelector);
        }
    }

    private IQueryable<PaymentListDto> GetPaymentListQueryable(InvoiceListRequest request)
    {
        var query = context.PaymentMeans
            .Where(x => !x.IsDeleted)
            .Select(s => new PaymentListDto()
            {
                PlaymentMeansId = s.PaymentMeansId,
                Iban = s.Iban,
                Name = s.Name,
            });

        if (!string.IsNullOrEmpty(request.Filter))
        {
            var filter = request.Filter.ToLower();
            query = query.Where(i =>
                i.Name.Contains(filter) ||
                i.Iban.Contains(filter));
        }

        return query;
    }

    public async Task<PaymentAnnotationDto> GetPaymentMeans(int paymentMeansId, CancellationToken token = default)
    {
        var paymentMeansEntity = await context.PaymentMeans
            .FirstOrDefaultAsync(f => f.PaymentMeansId == paymentMeansId, token);
        ArgumentNullException.ThrowIfNull(paymentMeansEntity, nameof(paymentMeansEntity));
        return new PaymentAnnotationDto()
        {
            Iban = paymentMeansEntity.Iban,
            Bic = paymentMeansEntity.Bic,
            Name = paymentMeansEntity.Name,
            PaymentMeansTypeCode = paymentMeansEntity.PaymentMeansTypeCode,
        };
    }

    public async Task<int> CreatePaymentMeans(IPaymentMeansBaseDto paymentMeans, CancellationToken token = default)
    {
        var paymentMeansEntity = new PaymentMeans()
        {
            Iban = paymentMeans.Iban,
            Bic = paymentMeans.Bic,
            Name = paymentMeans.Name,
            PaymentMeansTypeCode = paymentMeans.PaymentMeansTypeCode,
        };
        context.PaymentMeans.Add(paymentMeansEntity);
        await context.SaveChangesAsync(token);
        return paymentMeansEntity.PaymentMeansId;
    }

    public async Task UpdatePaymentMeans(int paymentMeansId, IPaymentMeansBaseDto paymentMeans, CancellationToken token = default)
    {
        var paymentMeansEntity = await context.PaymentMeans
            .FirstOrDefaultAsync(f => f.PaymentMeansId == paymentMeansId, token);
        ArgumentNullException.ThrowIfNull(paymentMeansEntity, nameof(paymentMeansEntity));
        paymentMeansEntity.Iban = paymentMeans.Iban;
        paymentMeansEntity.Bic = paymentMeans.Bic;
        paymentMeansEntity.Name = paymentMeans.Name;
        paymentMeansEntity.PaymentMeansTypeCode = paymentMeans.PaymentMeansTypeCode;
        await context.SaveChangesAsync(token);
    }

    public async Task DeletePaymentMeans(int paymentMeansId, CancellationToken token = default)
    {
        var paymentMeansEntity = await context.PaymentMeans
            .FirstOrDefaultAsync(f => f.PaymentMeansId == paymentMeansId, token);
        ArgumentNullException.ThrowIfNull(paymentMeansEntity, nameof(paymentMeansEntity));

        bool isReferences = await context.Invoices.AnyAsync(a => a.PaymentMeansId == paymentMeansId, token);

        if (isReferences)
        {
            paymentMeansEntity.IsDeleted = true;
        }
        else
        {
            context.PaymentMeans.Remove(paymentMeansEntity);
        }
        await context.SaveChangesAsync(token);
    }
}