using beinx.shared;
using beinx.shared.Interfaces;
using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;

namespace beinx.db.Services;

public abstract class BasePartyRepository<TDto> : IBasePartyRepository<TDto>, IDraftRepository<TDto> where TDto : IPartyBaseDto
{
    protected readonly IIndexedDbInterop _interop;
    protected readonly bool _isSeller;

    protected BasePartyRepository(IIndexedDbInterop interop, bool isSeller)
    {
        _interop = interop;
        _isSeller = isSeller;
    }

    public Task<int> CreateAsync(TDto dto)
        => _interop.CallAsync<int>("partyRepository.createParty", dto, _isSeller);

    // Update an existing party
    public async Task UpdateAsync(int id, IPartyBaseDto dto)
        => await _interop.CallVoidAsync("partyRepository.updateParty", id, dto, _isSeller);

    // Delete a party
    public async Task DeleteAsync(int id)
        => await _interop.CallVoidAsync("partyRepository.deleteParty", id, _isSeller);

    // Get all parties (sellers or buyers)
    public Task<List<PartyEntity<TDto>>> GetAllAsync()
        => _interop.CallAsync<List<PartyEntity<TDto>>>("partyRepository.getAllParties", _isSeller);

    public Task SetPartyLogo(int id, DocumentReferenceAnnotationDto? logo)
        => _interop.CallVoidAsync("partyRepository.setPartyLogo", id, logo, _isSeller);

    // Clear all parties from both stores
    public async Task Clear()
        => await _interop.CallVoidAsync("partyRepository.clear");

    public async Task<DraftState<TDto>?> LoadDraftAsync()
    {
        var draft = await _interop.CallAsync<Draft<TDto>>("partyRepository.loadTempParty", _isSeller);
        if (draft == null)
            return null;

        return new DraftState<TDto>
        {
            EntityId = draft.EntityId,
            Data = draft.Data
        };
    }

    public async Task SaveDraftAsync(DraftState<TDto> draft)
    {
        await _interop.CallVoidAsync("partyRepository.saveTempParty", draft.Data, _isSeller, draft.EntityId ?? default);
    }

    public async Task ClearDraftAsync(int? entityId)
    {
        await _interop.CallVoidAsync("partyRepository.clearTempParty", _isSeller, entityId ?? default);
    }
}

public class SellerRepository : BasePartyRepository<SellerAnnotationDto>, ISellerRepository
{
    public SellerRepository(IIndexedDbInterop interop)
        : base(interop, isSeller: true)
    {
    }
}

public class BuyerRepository : BasePartyRepository<BuyerAnnotationDto>, IBuyerRepository
{
    public BuyerRepository(IIndexedDbInterop interop)
        : base(interop, isSeller: false)
    {
    }
}
