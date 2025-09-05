using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using pax.XRechnung.NET;
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

    public async Task<DocumentReferenceAnnotationDto?> AddReplaceOrDeleteSellerLogo(int invoiceId, string? base64String, CancellationToken token = default)
    {
        try
        {
            var result = await _js.InvokeAsync<DocumentReferenceAnnotationDto?>("invoiceRepository.addReplaceOrDeleteSellerLogo", token, invoiceId, base64String);
            logger.LogInformation("Updated seller logo for invoice {InvoiceId}", invoiceId);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating seller logo for invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<int> CreateInvoice(BlazorInvoiceDto invoiceDto, int sellerId, int buyerId, int paymentId, bool isImported = false, CancellationToken token = default)
    {
        try
        {
            var invoiceId = await _js.InvokeAsync<int>("invoiceRepository.createInvoice", token, invoiceDto, sellerId, buyerId, paymentId, isImported);
            logger.LogInformation("Created invoice {InvoiceId}", invoiceId);
            return invoiceId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating invoice");
            throw;
        }
    }

    public async Task DeleteInvoice(int invoiceId, CancellationToken token = default)
    {
        try
        {
            await _js.InvokeVoidAsync("invoiceRepository.deleteInvoice", token, invoiceId);
            logger.LogInformation("Deleted invoice {InvoiceId}", invoiceId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<InvoiceDtoInfo?> GetInvoice(int invoiceId, CancellationToken token = default)
    {
        try
        {
            var invoice = await _js.InvokeAsync<InvoiceDtoInfo?>("invoiceRepository.getInvoice", token, invoiceId);
            logger.LogDebug("Retrieved invoice {InvoiceId}", invoiceId);
            return invoice;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<List<InvoiceListDto>> GetInvoices(InvoiceListRequest request, CancellationToken token = default)
    {
        try
        {
            var allInvoices = await _js.InvokeAsync<List<InvoiceListDto>>("invoiceRepository.getAllInvoices", token);

            // Apply filtering, sorting, and pagination in C#
            var filtered = allInvoices.AsEnumerable();

            if (request.Unpaid)
            {
                filtered = filtered.Where(x => !x.IsPaid);
            }

            if (!string.IsNullOrEmpty(request.Filter))
            {
                var filter = request.Filter.ToLowerInvariant();
                filtered = filtered.Where(i =>
                    i.BuyerEmail.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
            }

            var sorted = ApplyInvoiceSorting(filtered, request.TableOrders);
            var result = sorted.Skip(request.Skip).Take(request.Take).ToList();

            logger.LogDebug("Retrieved {Count} invoices (filtered from {Total})", result.Count, allInvoices.Count);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving invoices");
            throw;
        }
    }

    public async Task<int> GetInvoicesCount(InvoiceListRequest request, CancellationToken token = default)
    {
        try
        {
            var allInvoices = await _js.InvokeAsync<List<InvoiceListDto>>("invoiceRepository.getAllInvoices", token);

            var filtered = allInvoices.AsEnumerable();

            if (request.Unpaid)
            {
                filtered = filtered.Where(x => !x.IsPaid);
            }

            if (!string.IsNullOrEmpty(request.Filter))
            {
                var filter = request.Filter.ToLowerInvariant();
                filtered = filtered.Where(i =>
                    i.BuyerEmail.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
            }

            var count = filtered.Count();
            logger.LogDebug("Invoices count: {Count} (filtered from {Total})", count, allInvoices.Count);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error counting invoices");
            throw;
        }
    }

    private static IEnumerable<InvoiceListDto> ApplyInvoiceSorting(IEnumerable<InvoiceListDto> invoices, List<TableOrder> orders)
    {
        if (!orders.Any())
        {
            return invoices.OrderByDescending(i => i.IssueDate);
        }

        IOrderedEnumerable<InvoiceListDto>? orderedInvoices = null;

        for (int i = 0; i < orders.Count; i++)
        {
            var order = orders[i];
            var propertyName = order.PropertyName.ToLowerInvariant();

            if (i == 0)
            {
                orderedInvoices = propertyName switch
                {
                    "id" => order.Ascending ? invoices.OrderBy(p => p.Id) : invoices.OrderByDescending(p => p.Id),
                    "buyeremail" => order.Ascending ? invoices.OrderBy(p => p.BuyerEmail) : invoices.OrderByDescending(p => p.BuyerEmail),
                    "issuedate" => order.Ascending ? invoices.OrderBy(p => p.IssueDate) : invoices.OrderByDescending(p => p.IssueDate),
                    _ => invoices.OrderByDescending(p => p.IssueDate)
                };
            }
            else
            {
                orderedInvoices = propertyName switch
                {
                    "id" => order.Ascending ? orderedInvoices!.ThenBy(p => p.Id) : orderedInvoices!.ThenByDescending(p => p.Id),
                    "buyeremail" => order.Ascending ? orderedInvoices!.ThenBy(p => p.BuyerEmail) : orderedInvoices!.ThenByDescending(p => p.BuyerEmail),
                    "issuedate" => order.Ascending ? orderedInvoices!.ThenBy(p => p.IssueDate) : orderedInvoices!.ThenByDescending(p => p.IssueDate),
                    _ => orderedInvoices!.ThenByDescending(p => p.IssueDate)
                };
            }
        }

        return orderedInvoices ?? invoices.OrderByDescending(i => i.IssueDate);
    }

    public async Task UpdateInvoice(int invoiceId, BlazorInvoiceDto invoiceDto, CancellationToken token = default)
    {
        try
        {
            await _js.InvokeVoidAsync("invoiceRepository.updateInvoice", token, invoiceId, invoiceDto);
            logger.LogInformation("Updated invoice {InvoiceId}", invoiceId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<FinalizeResult> FinalizeInvoice(int invoiceId, XmlInvoice xmlInvoice, CancellationToken token = default)
    {
        try
        {
            var xmlText = XmlInvoiceWriter.Serialize(xmlInvoice);
            ArgumentNullException.ThrowIfNull(xmlText, nameof(xmlText));
            var bytes = Encoding.UTF8.GetBytes(xmlText);
            var hash = SHA1.HashData(bytes);
            var result = await _js.InvokeAsync<FinalizeResult>("invoiceRepository.finalizeInvoice", token, invoiceId, bytes, hash, xmlInvoice.LegalMonetaryTotal.TaxExclusiveAmount.Value);
            logger.LogInformation("Finalized invoice {InvoiceId}", invoiceId);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finalizing invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<XmlInvoice?> GetXmlInvoice(int invoiceId, CancellationToken token = default)
    {
        try
        {
            var xmlBlob = await _js.InvokeAsync<byte[]?>("invoiceRepository.getXmlBlob", token, invoiceId);
            if (xmlBlob == null)
            {
                return null;
            }
            var serializer = new XmlSerializer(typeof(XmlInvoice));
            using var stream = new MemoryStream(xmlBlob);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var xmlInvoice = (XmlInvoice?)serializer.Deserialize(reader);
            return xmlInvoice;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving XML invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<bool> ValidateXmlInvoiceHash(int invoiceId, CancellationToken token = default)
    {
        try
        {
            var isValid = await _js.InvokeAsync<bool>("invoiceRepository.validateXmlInvoiceHash", token, invoiceId);
            logger.LogInformation("Validated XML invoice hash for {InvoiceId}: {IsValid}", invoiceId, isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating XML invoice hash for {InvoiceId}", invoiceId);
            throw;
        }
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

    public async Task SetIsPaid(int invoiceId, bool isPaid, CancellationToken token = default)
    {
        try
        {
            await _js.InvokeVoidAsync("invoiceRepository.setIsPaid", token, invoiceId, isPaid);
            logger.LogInformation("Set isPaid to {IsPaid} for invoice {InvoiceId}", isPaid, invoiceId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting isPaid for invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<int> CreateInvoiceCopy(int invoiceId)
    {
        try
        {
            var invoice = await _js.InvokeAsync<InvoiceDtoInfo?>("invoiceRepository.getInvoice", invoiceId);
            ArgumentNullException.ThrowIfNull(invoice);
            if (!invoice.SellerId.HasValue)
            {
                throw new ArgumentNullException(nameof(invoice.SellerId));
            }

            if (!invoice.BuyerId.HasValue)
            {
                throw new ArgumentNullException(nameof(invoice.BuyerId));
            }

            if (!invoice.PaymentId.HasValue)
            {
                throw new ArgumentNullException(nameof(invoice.PaymentId));
            }


            invoice.InvoiceDto.Id = invoice.InvoiceDto.Id + "_copy";
            invoice.InvoiceDto.IssueDate = DateTime.UtcNow;
            invoice.InvoiceDto.DueDate = DateTime.UtcNow.AddDays(14);
            invoice.InvoiceDto.InvoiceLines.Clear();
            invoice.InvoiceDto.AdditionalDocumentReferences.Clear();

            var newInvoiceId = await CreateInvoice(invoice.InvoiceDto, invoice.SellerId.Value, invoice.BuyerId.Value, invoice.PaymentId.Value);
            logger.LogInformation("Created copy of invoice {InvoiceId} with new id {NewInvoiceId}", invoiceId, newInvoiceId);
            return newInvoiceId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating copy of invoice {InvoiceId}", invoiceId);
            throw;
        }
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