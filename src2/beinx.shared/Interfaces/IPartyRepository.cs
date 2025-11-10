using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;

namespace beinx.shared.Interfaces;

public interface IBasePartyRepository<TDto> where TDto : IPartyBaseDto
{
    Task Clear();
    Task ClearDraftAsync(int? entityId);
    Task<int> CreateAsync(TDto dto);
    Task DeleteAsync(int id);
    Task<List<PartyEntity<TDto>>> GetAllAsync();
    Task<DraftState<TDto>?> LoadDraftAsync();
    Task SaveDraftAsync(DraftState<TDto> draft);
    Task UpdateAsync(int id, IPartyBaseDto dto);
}

public interface ISellerRepository : IBasePartyRepository<SellerAnnotationDto> { }
public interface IBuyerRepository : IBasePartyRepository<BuyerAnnotationDto> { }