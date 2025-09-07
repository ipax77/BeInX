
using BlazorInvoice.Shared;
using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;

namespace BlazorInvoice.IndexedDb.Services
{
    public partial class InvoiceRepository
    {
        // Parties
        public async Task AddReplaceOrDeletePartyLogo(string? base64String, int partyId)
        {
            var party = await _indexedDbService.GetParty(partyId);
            if (party != null)
            {
                if (string.IsNullOrEmpty(base64String))
                {
                    party.Logo = null;
                }
                else
                {
                    party.Logo = new DocumentReferenceAnnotationDto
                    {
                        Content = base64String,
                        Id = Guid.NewGuid().ToString(),
                        DocumentDescription = "Party Logo",
                        MimeCode = "image/png",
                        FileName = "logo.png"
                    };
                }
                await _indexedDbService.UpdateParty(party);
            }
        }

        public async Task<int> CreateParty(IPartyBaseDto party, bool isSeller, CancellationToken token = default)
        {
            return await _indexedDbService.CreateParty(party, isSeller);
        }

        public async Task DeleteParty(int partyId, CancellationToken token = default)
        {
            await _indexedDbService.DeleteParty(partyId);
        }

        public async Task<BuyerAnnotationDto?> GetBuyer(int partyId, CancellationToken token = default)
        {
            var party = await _indexedDbService.GetParty(partyId);
            return ToBuyerAnnotationDto(party);
        }

        public async Task<List<PartyListDto>> GetBuyers(InvoiceListRequest request, CancellationToken token = default)
        {
            var parties = await _indexedDbService.GetAllParties();
            var buyers = parties.Where(p => !p.IsSeller).AsEnumerable();

            if (!string.IsNullOrEmpty(request.Filter))
            {
                var filter = request.Filter.ToLowerInvariant();
                buyers = buyers.Where(p =>
                    p.Party.Name.ToLowerInvariant().Contains(filter) ||
                    p.Party.Email.ToLowerInvariant().Contains(filter));
            }

            var sorted = ApplyPartySorting(buyers, request.TableOrders);
            var result = sorted.Skip(request.Skip).Take(request.Take).Select(ToPartyListDto).ToList();

            return result;
        }

        public async Task<int> GetBuyersCount(InvoiceListRequest request, CancellationToken token = default)
        {
            var parties = await _indexedDbService.GetAllParties();
            var buyers = parties.Where(p => !p.IsSeller).AsEnumerable();

            if (!string.IsNullOrEmpty(request.Filter))
            {
                var filter = request.Filter.ToLowerInvariant();
                buyers = buyers.Where(p =>
                    p.Party.Name.ToLowerInvariant().Contains(filter) ||
                    p.Party.Email.ToLowerInvariant().Contains(filter));
            }

            return buyers.Count();
        }

        public async Task<DocumentReferenceAnnotationDto?> GetPartyLogo(int partyId, CancellationToken token = default)
        {
            var party = await _indexedDbService.GetParty(partyId);
            return party?.Logo;
        }

        public async Task<SellerAnnotationDto?> GetSeller(int partyId, CancellationToken token = default)
        {
            var party = await _indexedDbService.GetParty(partyId);
            return ToSellerAnnotationDto(party);
        }

        public async Task<List<PartyListDto>> GetSellers(InvoiceListRequest request, CancellationToken token = default)
        {
            var parties = await _indexedDbService.GetAllParties();
            var sellers = parties.Where(p => p.IsSeller).AsEnumerable();

            if (!string.IsNullOrEmpty(request.Filter))
            {
                var filter = request.Filter.ToLowerInvariant();
                sellers = sellers.Where(p =>
                    p.Party.Name.ToLowerInvariant().Contains(filter) ||
                    p.Party.Email.ToLowerInvariant().Contains(filter));
            }

            var sorted = ApplyPartySorting(sellers, request.TableOrders);
            var result = sorted.Skip(request.Skip).Take(request.Take).Select(ToPartyListDto).ToList();

            return result;
        }

        public async Task<int> GetSellersCount(InvoiceListRequest request, CancellationToken token = default)
        {
            var parties = await _indexedDbService.GetAllParties();
            var sellers = parties.Where(p => p.IsSeller).AsEnumerable();

            if (!string.IsNullOrEmpty(request.Filter))
            {
                var filter = request.Filter.ToLowerInvariant();
                sellers = sellers.Where(p =>
                    p.Party.Name.ToLowerInvariant().Contains(filter) ||
                    p.Party.Email.ToLowerInvariant().Contains(filter));
            }

            return sellers.Count();
        }

        public async Task UpdateParty(int partyId, IPartyBaseDto party, CancellationToken token = default)
        {
            var entity = await _indexedDbService.GetParty(partyId);
            if (entity != null)
            {
                entity.Party = party;
                await _indexedDbService.UpdateParty(entity);
            }
        }

        // Mappers
        private BuyerAnnotationDto? ToBuyerAnnotationDto(PartyEntity? entity)
        {
            if (entity == null) return null;
            return new BuyerAnnotationDto
            {
                Name = entity.Party.Name,
                City = entity.Party.City,
                StreetName = entity.Party.StreetName,
                PostCode = entity.Party.PostCode,
                CountryCode = entity.Party.CountryCode,
                RegistrationName = entity.Party.RegistrationName,
                TaxId = entity.Party.TaxId,
                CompanyId = entity.Party.CompanyId,
                Email = entity.Party.Email,
                Telefone = entity.Party.Telefone,
                Website = entity.Party.Website,
                BuyerReference = entity.Party.BuyerReference,
                LogoReferenceId = entity.Logo?.Id
            };
        }

        private SellerAnnotationDto? ToSellerAnnotationDto(PartyEntity? entity)
        {
            if (entity == null) return null;
            return new SellerAnnotationDto
            {
                Name = entity.Party.Name,
                City = entity.Party.City,
                StreetName = entity.Party.StreetName,
                PostCode = entity.Party.PostCode,
                CountryCode = entity.Party.CountryCode,
                RegistrationName = entity.Party.RegistrationName,
                TaxId = entity.Party.TaxId,
                CompanyId = entity.Party.CompanyId,
                Email = entity.Party.Email,
                Telefone = entity.Party.Telefone,
                Website = entity.Party.Website,
                LogoReferenceId = entity.Logo?.Id
            };
        }

        private PartyListDto ToPartyListDto(PartyEntity? entity)
        {
            if (entity == null) return new();
            return new PartyListDto
            {
                Name = entity.Party.Name,
                Email = entity.Party.Email
            };
        }

        private static IEnumerable<PartyEntity> ApplyPartySorting(IEnumerable<PartyEntity> parties, List<TableOrder> orders)
        {
            if (orders.Count == 0)
            {
                return parties.OrderBy(p => p.Party.Name);
            }

            IOrderedEnumerable<PartyEntity>? orderedParties = null;

            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                var propertyName = order.PropertyName.ToLowerInvariant();

                if (orderedParties is null)
                {
                    orderedParties = propertyName switch
                    {
                        "name" => order.Ascending ? parties.OrderBy(p => p.Party.Name) : parties.OrderByDescending(p => p.Party.Name),
                        "email" => order.Ascending ? parties.OrderBy(p => p.Party.Email) : parties.OrderByDescending(p => p.Party.Email),
                        _ => parties.OrderBy(p => p.Party.Name)
                    };
                }
                else
                {
                    orderedParties = propertyName switch
                    {
                        "name" => order.Ascending ? orderedParties.ThenBy(p => p.Party.Name) : orderedParties.ThenByDescending(p => p.Party.Name),
                        "email" => order.Ascending ? orderedParties.ThenBy(p => p.Party.Email) : orderedParties.ThenByDescending(p => p.Party.Email),
                        _ => orderedParties.ThenBy(p => p.Party.Name)
                    };
                }
            }

            return orderedParties ?? parties.OrderBy(p => p.Party.Name);
        }
    }
}
