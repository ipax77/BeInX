using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;
using pax.XRechnung.NET.XmlModels;

namespace BlazorInvoice.IndexedDb.Services;

public partial class InvoiceRepository(IJSRuntime _js, ILogger<InvoiceRepository> logger) : IInvoiceRepository
{
    // Party Logo Operations
    public async Task AddReplaceOrDeletePartyLogo(string? base64String, int partyId)
    {
        try
        {
            if (string.IsNullOrEmpty(base64String))
            {
                // Delete logo by setting empty string
                await _js.InvokeVoidAsync("partyRepository.updatePartyLogo", partyId, "", null);
                logger.LogInformation("Deleted logo for party {PartyId}", partyId);
            }
            else
            {
                // Add or replace logo
                var logoReferenceId = Guid.NewGuid().ToString();
                await _js.InvokeVoidAsync("partyRepository.updatePartyLogo", partyId, base64String, logoReferenceId);
                logger.LogInformation("Updated logo for party {PartyId}", partyId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating logo for party {PartyId}", partyId);
            throw;
        }
    }

    // Party CRUD Operations
    public async Task<int> CreateParty(IPartyBaseDto party, bool isSeller, CancellationToken token = default)
    {
        try
        {
            var partyId = await _js.InvokeAsync<int>("partyRepository.createParty", token, party, isSeller);
            logger.LogInformation("Created {PartyType} party {PartyId}: {PartyName}",
                isSeller ? "seller" : "buyer", partyId, party.Name);
            return partyId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating {PartyType} party: {PartyName}",
                isSeller ? "seller" : "buyer", party.Name);
            throw;
        }
    }

    public async Task DeleteParty(int partyId, CancellationToken token = default)
    {
        try
        {
            // Check if party is referenced by invoices to determine soft vs hard delete
            var referencedIds = await _js.InvokeAsync<int[]>("partyRepository.getReferencedPartyIds", token);
            var isReferenced = referencedIds.Contains(partyId);

            await _js.InvokeVoidAsync("partyRepository.deleteParty", token, partyId, isReferenced);

            logger.LogInformation("Deleted party {PartyId} ({DeleteType})",
                partyId, isReferenced ? "soft delete" : "hard delete");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting party {PartyId}", partyId);
            throw;
        }
    }

    public async Task UpdateParty(int partyId, IPartyBaseDto party, CancellationToken token = default)
    {
        try
        {
            await _js.InvokeVoidAsync("partyRepository.updateParty", token, partyId, party);
            logger.LogInformation("Updated party {PartyId}: {PartyName}", partyId, party.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating party {PartyId}", partyId);
            throw;
        }
    }

    // Seller Operations
    public async Task<SellerAnnotationDto?> GetSeller(int partyId, CancellationToken token = default)
    {
        try
        {
            var seller = await _js.InvokeAsync<SellerAnnotationDto?>("partyRepository.getSeller", token, partyId);
            logger.LogDebug("Retrieved seller {PartyId}", partyId);
            return seller;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving seller {PartyId}", partyId);
            throw;
        }
    }

    public async Task<List<PartyListDto>> GetSellers(InvoiceListRequest request, CancellationToken token = default)
    {
        try
        {
            // Get all sellers from IndexedDB
            var allSellers = await _js.InvokeAsync<PartyListDto[]>("partyRepository.getAllParties", token, true);

            // Apply filtering in C#
            var filtered = allSellers.AsEnumerable();
            if (!string.IsNullOrEmpty(request.Filter))
            {
                var filter = request.Filter.ToLowerInvariant();
                filtered = filtered.Where(s =>
                    s.Name.ToLowerInvariant().Contains(filter) ||
                    s.Email.ToLowerInvariant().Contains(filter));
            }

            // Apply sorting in C#
            var sorted = ApplyPartySorting(filtered, request.TableOrders);

            // Apply pagination in C#
            var result = sorted.Skip(request.Skip).Take(request.Take).ToList();

            logger.LogDebug("Retrieved {Count} sellers (filtered from {Total})", result.Count, allSellers.Length);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving sellers");
            throw;
        }
    }

    public async Task<int> GetSellersCount(InvoiceListRequest request, CancellationToken token = default)
    {
        try
        {
            // Get all sellers from IndexedDB
            var allSellers = await _js.InvokeAsync<PartyListDto[]>("partyRepository.getAllParties", token, true);

            // Apply filtering in C#
            var filtered = allSellers.AsEnumerable();
            if (!string.IsNullOrEmpty(request.Filter))
            {
                var filter = request.Filter.ToLowerInvariant();
                filtered = filtered.Where(s =>
                    s.Name.ToLowerInvariant().Contains(filter) ||
                    s.Email.ToLowerInvariant().Contains(filter));
            }

            var count = filtered.Count();
            logger.LogDebug("Sellers count: {Count} (filtered from {Total})", count, allSellers.Length);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error counting sellers");
            throw;
        }
    }

    // Buyer Operations
    public async Task<BuyerAnnotationDto?> GetBuyer(int partyId, CancellationToken token = default)
    {
        try
        {
            var buyer = await _js.InvokeAsync<BuyerAnnotationDto?>("partyRepository.getBuyer", token, partyId);
            logger.LogDebug("Retrieved buyer {PartyId}", partyId);
            return buyer;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving buyer {PartyId}", partyId);
            throw;
        }
    }

    public async Task<List<PartyListDto>> GetBuyers(InvoiceListRequest request, CancellationToken token = default)
    {
        try
        {
            // Get all buyers from IndexedDB
            var allBuyers = await _js.InvokeAsync<PartyListDto[]>("partyRepository.getAllParties", token, false);

            // Apply filtering in C#
            var filtered = allBuyers.AsEnumerable();
            if (!string.IsNullOrEmpty(request.Filter))
            {
                var filter = request.Filter.ToLowerInvariant();
                filtered = filtered.Where(b =>
                    b.Name.ToLowerInvariant().Contains(filter) ||
                    b.Email.ToLowerInvariant().Contains(filter));
            }

            // Apply sorting in C#
            var sorted = ApplyPartySorting(filtered, request.TableOrders);

            // Apply pagination in C#
            var result = sorted.Skip(request.Skip).Take(request.Take).ToList();

            logger.LogDebug("Retrieved {Count} buyers (filtered from {Total})", result.Count, allBuyers.Length);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving buyers");
            throw;
        }
    }

    public async Task<int> GetBuyersCount(InvoiceListRequest request, CancellationToken token = default)
    {
        try
        {
            // Get all buyers from IndexedDB
            var allBuyers = await _js.InvokeAsync<PartyListDto[]>("partyRepository.getAllParties", token, false);

            // Apply filtering in C#
            var filtered = allBuyers.AsEnumerable();
            if (!string.IsNullOrEmpty(request.Filter))
            {
                var filter = request.Filter.ToLowerInvariant();
                filtered = filtered.Where(b =>
                    b.Name.ToLowerInvariant().Contains(filter) ||
                    b.Email.ToLowerInvariant().Contains(filter));
            }

            var count = filtered.Count();
            logger.LogDebug("Buyers count: {Count} (filtered from {Total})", count, allBuyers.Length);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error counting buyers");
            throw;
        }
    }

    // Party Logo Operations
    public async Task<DocumentReferenceAnnotationDto?> GetPartyLogo(int partyId, CancellationToken token = default)
    {
        try
        {
            var logo = await _js.InvokeAsync<DocumentReferenceAnnotationDto?>("partyRepository.getPartyLogo", token, partyId);
            logger.LogDebug("Retrieved logo for party {PartyId}: {HasLogo}", partyId, logo != null);
            return logo;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving logo for party {PartyId}", partyId);
            throw;
        }
    }

    // Helper method for sorting parties
    private static IEnumerable<PartyListDto> ApplyPartySorting(IEnumerable<PartyListDto> parties, List<TableOrder> orders)
    {
        if (!orders.Any())
        {
            return parties.OrderBy(p => p.Name);
        }

        IOrderedEnumerable<PartyListDto>? orderedParties = null;

        for (int i = 0; i < orders.Count; i++)
        {
            var order = orders[i];
            var propertyName = order.PropertyName.ToLowerInvariant();

            if (i == 0)
            {
                orderedParties = propertyName switch
                {
                    "name" => order.Ascending
                        ? parties.OrderBy(p => p.Name)
                        : parties.OrderByDescending(p => p.Name),
                    "email" => order.Ascending
                        ? parties.OrderBy(p => p.Email)
                        : parties.OrderByDescending(p => p.Email),
                    _ => parties.OrderBy(p => p.Name)
                };
            }
            else
            {
                orderedParties = propertyName switch
                {
                    "name" => order.Ascending
                        ? orderedParties!.ThenBy(p => p.Name)
                        : orderedParties!.ThenByDescending(p => p.Name),
                    "email" => order.Ascending
                        ? orderedParties!.ThenBy(p => p.Email)
                        : orderedParties!.ThenByDescending(p => p.Email),
                    _ => orderedParties!.ThenBy(p => p.Name)
                };
            }
        }

        return orderedParties ?? parties.OrderBy(p => p.Name);
    }

    // Payment-related methods (to be implemented separately)
    public async Task<int> CreatePaymentMeans(IPaymentMeansBaseDto paymentMeans, CancellationToken token = default)
    {
        try
        {
            return await _js.InvokeAsync<int>("createPaymentMeans", paymentMeans);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed creating payment: {error}", ex.Message);
        }
        return 0;
    }

    public async Task DeletePaymentMeans(int paymentMeansId, CancellationToken token = default)
    {
        try
        {
            await _js.InvokeVoidAsync("deletePaymentMeans", paymentMeansId);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed deleting payments: {error}", ex.Message);
        }
    }

    public async Task<PaymentAnnotationDto> GetPaymentMeans(int paymentMeansId, CancellationToken token = default)
    {
        try
        {
            return await _js.InvokeAsync<PaymentAnnotationDto>("getPaymentMeans", paymentMeansId);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting payments: {error}", ex.Message);
        }
        return new();
    }

    public async Task<List<PaymentListDto>> GetPayments(InvoiceListRequest request, CancellationToken token = default)
    {
        try
        {
            return await _js.InvokeAsync<List<PaymentListDto>>("getPayments", request);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting payments: {error}", ex.Message);
        }
        return [];
    }

    public async Task<int> GetPaymentsCount(InvoiceListRequest request, CancellationToken token = default)
    {
        try
        {
            return await _js.InvokeAsync<int>("getPaymentsCount", request);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting payments count: {error}", ex.Message);
        }
        return 0;
    }

    public async Task UpdatePaymentMeans(int paymentMeansId, IPaymentMeansBaseDto paymentMeans, CancellationToken token = default)
    {
        try
        {
            await _js.InvokeVoidAsync("updatePaymentMeans", paymentMeansId, paymentMeans);
        }
        catch (Exception ex)
        {
            logger.LogError("Update payments failed: {error}", ex.Message);
        }
    }

    public Task<DocumentReferenceAnnotationDto?> AddReplaceOrDeletePdf(string? base64String, int invoiceId)
    {
        throw new NotImplementedException();
    }

    public Task<DocumentReferenceAnnotationDto?> AddReplaceOrDeleteSellerLogo(int invoiceId, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<int> CreateInvoice(BlazorInvoiceDto invoiceDto, int sellerId, int buyerId, int paymentId, bool isImported = false, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteInvoice(int invoiceId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<InvoiceDtoInfo?> GetInvoice(int invoiceId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<InvoiceListDto>> GetInvoices(InvoiceListRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetInvoicesCount(InvoiceListRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateInvoice(int invoiceId, BlazorInvoiceDto invoiceDto, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<FinalizeResult> FinalizeInvoice(int invoiceId, XmlInvoice xmlInvoice, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<XmlInvoice?> GetXmlInvoice(int invoiceId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ValidateXmlInvoiceHash(int invoiceId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HasTempInvoice()
    {
        throw new NotImplementedException();
    }

    public Task DeleteTempInvoice()
    {
        throw new NotImplementedException();
    }

    public Task SaveTempInvoice(InvoiceDtoInfo request)
    {
        throw new NotImplementedException();
    }

    public Task<InvoiceDtoInfo?> GetTempInvoice()
    {
        throw new NotImplementedException();
    }

    public Task<int> ImportInvoice(XmlInvoice invoice, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<ExportResult> ExportInvoice(int invoiceId)
    {
        throw new NotImplementedException();
    }

    public Task SetIsPaid(int invoiceId, bool isPaid, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> CreateInvoiceCopy(int invoiceId)
    {
        throw new NotImplementedException();
    }

    public Task SeedTestInvoices(int count)
    {
        throw new NotImplementedException();
    }

    public Task<int> SeedTestInvoice()
    {
        throw new NotImplementedException();
    }
}