using BlazorInvoice.Shared;
using Microsoft.EntityFrameworkCore;
using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;
using System.Linq.Expressions;

namespace BlazorInvoice.Db.Repository;

public partial class InvoiceRepository
{
    public async Task<int> GetSellersCount(InvoiceListRequest request, CancellationToken token = default)
    {
        var query = GetPartyListQueryable(request, true);
        return await query.CountAsync(token);
    }

    public async Task<List<PartyListDto>> GetSellers(InvoiceListRequest request, CancellationToken token = default)
    {
        var query = GetPartyListQueryable(request, true);
        query = GetPartySortedQueryable(query, request.TableOrders);
        return await query
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(token);
    }

    public async Task<int> GetBuyersCount(InvoiceListRequest request, CancellationToken token = default)
    {
        var query = GetPartyListQueryable(request, false);
        return await query.CountAsync(token);
    }

    public async Task<List<PartyListDto>> GetBuyers(InvoiceListRequest request, CancellationToken token = default)
    {
        var query = GetPartyListQueryable(request, false);
        query = GetPartySortedQueryable(query, request.TableOrders);
        return await query
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(token);
    }

    private static IOrderedQueryable<PartyListDto> GetPartySortedQueryable(IQueryable<PartyListDto> query, List<TableOrder> orders)
    {
        if (orders.Count == 0)
        {
            return query.OrderBy(i => i.Name);
        }

        IOrderedQueryable<PartyListDto>? orderedQuery = null;

        for (int i = 0; i < orders.Count; i++)
        {
            var order = orders[i];
            orderedQuery = ApplyPartyOrdering(query, orderedQuery, order.PropertyName.ToLowerInvariant(), order.Ascending, i == 0);
        }

        return orderedQuery ?? query.OrderBy(i => i.Name);
    }

    private static IOrderedQueryable<PartyListDto> ApplyPartyOrdering(
        IQueryable<PartyListDto> baseQuery,
        IOrderedQueryable<PartyListDto>? currentQuery,
        string property,
        bool ascending,
        bool isFirstOrder)
    {
        Expression<Func<PartyListDto, object>> keySelector = property switch
        {
            "name" => x => x.Name,
            "email" => x => x.Email,
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



    private IQueryable<PartyListDto> GetPartyListQueryable(InvoiceListRequest request, bool isSeller)
    {
        var query = context.InvoiceParties
            .Where(i => !i.IsDeleted && i.IsSeller == isSeller)
            .Select(s => new PartyListDto()
            {
                PartyId = s.InvoicePartyId,
                Name = s.Name,
                Email = s.Email
            });

        if (!string.IsNullOrEmpty(request.Filter))
        {
            query = query.Where(i => i.Name.Contains(request.Filter)
                || i.Email.Contains(request.Filter));
        }

        return query;
    }

    public async Task<DocumentReferenceAnnotationDto?> GetPartyLogo(int partyId, CancellationToken token = default)
    {
        var party = await context.InvoiceParties
            .FirstOrDefaultAsync(f => f.InvoicePartyId == partyId, token);
        if (party is null || party.Logo is null)
        {
            return null;
        }
        return new DocumentReferenceAnnotationDto()
        {
            Id = party.LogoReferenceId ?? Guid.NewGuid().ToString(),
            MimeCode = "image/png",
            DocumentDescription = "Seller Logo",
            Content = Convert.ToBase64String(party.Logo)
        };
    }

    public async Task<SellerAnnotationDto?> GetSeller(int partyId, CancellationToken token = default)
    {
        return await context.InvoiceParties
            .Where(x => x.InvoicePartyId == partyId)
            .Select(s => new SellerAnnotationDto()
            {
                Website = s.Website,
                LogoReferenceId = s.LogoReferenceId,
                Name = s.Name,
                StreetName = s.StreetName,
                City = s.City,
                PostCode = s.PostCode,
                CountryCode = s.CountryCode,
                Telefone = s.Telefone,
                Email = s.Email,
                RegistrationName = s.RegistrationName,
                TaxId = s.TaxId,
                CompanyId = s.CompanyId,
                BuyerReference = s.BuyerReference,
            })
            .FirstOrDefaultAsync(token);
    }

    public async Task<BuyerAnnotationDto?> GetBuyer(int partyId, CancellationToken token = default)
    {
        return await context.InvoiceParties
            .Where(x => x.InvoicePartyId == partyId)
            .Select(s => new BuyerAnnotationDto()
            {
                Website = s.Website,
                LogoReferenceId = s.LogoReferenceId,
                Name = s.Name,
                StreetName = s.StreetName,
                City = s.City,
                PostCode = s.PostCode,
                CountryCode = s.CountryCode,
                Telefone = s.Telefone,
                Email = s.Email,
                RegistrationName = s.RegistrationName,
                TaxId = s.TaxId,
                CompanyId = s.CompanyId,
                BuyerReference = s.BuyerReference,
            })
            .FirstOrDefaultAsync(token);
    }

    public async Task<int> CreateParty(IPartyBaseDto party, bool isSeller, CancellationToken token = default)
    {
        var newParty = new InvoiceParty()
        {
            Website = party.Website,
            LogoReferenceId = party.LogoReferenceId,
            Name = party.Name,
            StreetName = party.StreetName,
            City = party.City,
            PostCode = party.PostCode,
            CountryCode = party.CountryCode,
            Telefone = party.Telefone,
            Email = party.Email,
            RegistrationName = party.RegistrationName,
            TaxId = party.TaxId,
            CompanyId = party.CompanyId,
            BuyerReference = party.BuyerReference,
            IsSeller = isSeller
        };
        context.InvoiceParties.Add(newParty);
        await context.SaveChangesAsync(token);
        return newParty.InvoicePartyId;
    }

    public async Task UpdateParty(int partyId, IPartyBaseDto party, CancellationToken token = default)
    {
        var existingParty = await context.InvoiceParties
            .FirstOrDefaultAsync(f => f.InvoicePartyId == partyId, token);
        ArgumentNullException.ThrowIfNull(existingParty, nameof(existingParty));
        existingParty.Website = party.Website;
        existingParty.LogoReferenceId = party.LogoReferenceId;
        existingParty.Name = party.Name;
        existingParty.StreetName = party.StreetName;
        existingParty.City = party.City;
        existingParty.PostCode = party.PostCode;
        existingParty.CountryCode = party.CountryCode;
        existingParty.Telefone = party.Telefone;
        existingParty.Email = party.Email;
        existingParty.RegistrationName = party.RegistrationName;
        existingParty.TaxId = party.TaxId;
        existingParty.CompanyId = party.CompanyId;
        existingParty.BuyerReference = party.BuyerReference;
        await context.SaveChangesAsync(token);
    }

    public async Task DeleteParty(int partyId, CancellationToken token = default)
    {
        var existingParty = await context.InvoiceParties
            .FirstOrDefaultAsync(f => f.InvoicePartyId == partyId, token);

        ArgumentNullException.ThrowIfNull(existingParty, nameof(existingParty));

        // Check if this party is used in any invoice as buyer or seller
        bool isReferenced = await context.Invoices
            .AnyAsync(i => i.SellerPartyId == partyId || i.BuyerPartyId == partyId, token);

        if (isReferenced)
        {
            // Mark as deleted instead of removing
            existingParty.IsDeleted = true;
        }
        else
        {
            context.InvoiceParties.Remove(existingParty);
        }

        await context.SaveChangesAsync(token);
    }

}