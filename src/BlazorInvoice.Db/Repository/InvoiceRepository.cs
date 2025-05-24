using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
namespace BlazorInvoice.Db.Repository;

public partial class InvoiceRepository(InvoiceContext context, IConfigService configService) : IInvoiceRepository
{
    public async Task<int> GetInvoicesCount(InvoiceListRequest request, CancellationToken token = default)
    {
        var query = GetInvoiceListQueryable(request);
        return await query.CountAsync(token);
    }

    public async Task<List<InvoiceListDto>> GetInvoices(InvoiceListRequest request, CancellationToken token = default)

    {
        var query = GetInvoiceListQueryable(request);
        query = GetInvoiceListSortedQueryable(query, request.TableOrders);
        return await query
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(token);
    }

    private static IOrderedQueryable<InvoiceListDto> GetInvoiceListSortedQueryable(IQueryable<InvoiceListDto> query, List<TableOrder> orders)
    {
        if (orders.Count == 0)
            return query.OrderByDescending(i => i.IssueDate);

        IOrderedQueryable<InvoiceListDto>? orderedQuery = null;

        for (int i = 0; i < orders.Count; i++)
        {
            var order = orders[i];
            orderedQuery = ApplyOrdering(query, orderedQuery, order.PropertyName.ToLowerInvariant(), order.Ascending, i == 0);
        }

        return orderedQuery ?? query.OrderByDescending(i => i.IssueDate);
    }

    private static IOrderedQueryable<InvoiceListDto> ApplyOrdering(
        IQueryable<InvoiceListDto> baseQuery,
        IOrderedQueryable<InvoiceListDto>? currentQuery,
        string property,
        bool ascending,
        bool isFirstOrder)
    {
        Expression<Func<InvoiceListDto, object>> keySelector = property switch
        {
            "id" => x => x.Id,
            "issuedate" => x => x.IssueDate,
            "buyeremail" => x => x.BuyerEmail,
            _ => x => x.IssueDate
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


    private IQueryable<InvoiceListDto> GetInvoiceListQueryable(InvoiceListRequest request)
    {
        var query = context.Invoices.Select(s => new InvoiceListDto()
        {
            InvoiceId = s.InvoiceId,
            Id = s.Id,
            IssueDate = s.IssueDate,
            BuyerEmail = s.BuyerParty!.Email,
            IsFinalized = s.XmlInvoiceSha1Hash != null,
            IsPaid = s.IsPaid,
        });

        if (request.Unpaid)
        {
            query = query.Where(x => !x.IsPaid);
        }

        if (!string.IsNullOrEmpty(request.Filter))
        {
            query = query.Where(i => i.BuyerEmail.Contains(request.Filter));
        }

        return query;
    }
}