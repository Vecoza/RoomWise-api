using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public sealed class SearchService : ISearchService
{
    private readonly DbContext _db;

    public SearchService(DbContext db) => _db = db;

    public async Task<PagedResult<HotelSearchItemResponse>> SearchHotelsAsync(HotelSearchRequest req, CancellationToken ct = default)
    {

        var checkIn = req.CheckIn.Date;
        var checkOut = req.CheckOut.Date;
        if (checkOut <= checkIn) throw new ArgumentException("CheckOut must be after CheckIn.");
        if (req.Guests < 1) throw new ArgumentException("Guests must be >= 1.");


        var hotelsQ = _db.Set<Hotel>()
            .Include(h => h.City)
            .Include(h => h.Images)
            .AsQueryable();

        if (req.CityId.HasValue) hotelsQ = hotelsQ.Where(h => h.CityId == req.CityId.Value);
        if (!string.IsNullOrWhiteSpace(req.Q))
        {
            var q = req.Q.Trim();
            hotelsQ = hotelsQ.Where(h => EF.Functions.ILike(h.Name, $"%{q}%") ||
                                         EF.Functions.ILike(h.City.Name, $"%{q}%"));
        }


        var candidateHotels = await hotelsQ.ToListAsync(ct);
        if (candidateHotels.Count == 0)
            return Paged(candidateHotels, new List<HotelSearchItemResponse>(), req.Page, req.PageSize);

        var hotelIds = candidateHotels.Select(h => h.Id).ToList();

        // Preload tags
        var tagsLookup = await _db.Set<HotelTag>()
            .Include(ht => ht.Tag)
            .Where(ht => hotelIds.Contains(ht.HotelId))
            .GroupBy(ht => ht.HotelId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(x => new TagResponse { Id = x.TagId, Name = x.Tag.Name }).ToList(),
                ct);

        // Filter by facilities (must include all requested)
        if (req.FacilityIds is { Length: > 0 })
        {
            var requested = req.FacilityIds.Distinct().ToHashSet();
            var hotelFacilities = await _db.Set<HotelFacility>()
                .Where(hf => hotelIds.Contains(hf.HotelId))
                .ToListAsync(ct);

            var okHotels = hotelFacilities
                .GroupBy(hf => hf.HotelId)
                .Where(g => requested.IsSubsetOf(g.Select(x => x.FacilityId).ToHashSet()))
                .Select(g => g.Key)
                .ToHashSet();

            candidateHotels = candidateHotels.Where(h => okHotels.Contains(h.Id)).ToList();
            hotelIds = candidateHotels.Select(h => h.Id).ToList();
        }

        // Filter by add-ons (must include all requested)
        if (req.AddOnIds is { Length: > 0 })
        {
            var requested = req.AddOnIds.Distinct().ToHashSet();
            var addOns = await _db.Set<AddOn>()
                .Where(a => hotelIds.Contains(a.HotelId) && a.IsActive)
                .ToListAsync(ct);

            var okHotels = addOns
                .GroupBy(a => a.HotelId)
                .Where(g => requested.IsSubsetOf(g.Select(x => x.Id).ToHashSet()))
                .Select(g => g.Key)
                .ToHashSet();

            candidateHotels = candidateHotels.Where(h => okHotels.Contains(h.Id)).ToList();
            hotelIds = candidateHotels.Select(h => h.Id).ToList();
        }


        var roomTypes = await _db.Set<RoomType>()
            .Where(rt => hotelIds.Contains(rt.HotelId) && rt.Capacity >= req.Guests)
            .ToListAsync(ct);

        if (roomTypes.Count == 0)
            return Paged(candidateHotels, new List<HotelSearchItemResponse>(), req.Page, req.PageSize);

        var roomTypeIds = roomTypes.Select(rt => rt.Id).ToList();

        var rates = await _db.Set<RoomRate>()
            .Where(r => roomTypeIds.Contains(r.RoomTypeId)
                        && r.StartDate <= checkIn
                        && r.EndDate >= checkOut)
            .ToListAsync(ct);

        var hasRoomAvailability = _db.Model.FindEntityType(typeof(RoomAvailability)) is not null;

        Dictionary<int, List<Reservation>>? reservationsByRoomType = null;

        if (!hasRoomAvailability)
        {
            var minDate = checkIn;
            var maxDateExclusive = checkOut;

            var overlaps = await _db.Set<Reservation>()
                .Where(r => roomTypeIds.Contains(r.RoomTypeId)
                            && r.Status != "Cancelled"
                            && r.CheckIn < maxDateExclusive
                            && r.CheckOut > minDate)
                .ToListAsync(ct);

            reservationsByRoomType = overlaps
                .GroupBy(r => r.RoomTypeId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        else
        {

            await _db.Set<RoomAvailability>()
                .Where(a => roomTypeIds.Contains(a.RoomTypeId)
                            && a.Date >= checkIn
                            && a.Date < checkOut)
                .LoadAsync(ct);
        }

        var nightlyByRoomType = new Dictionary<int, decimal>();
        var ratesByRt = rates.GroupBy(r => r.RoomTypeId).ToDictionary(g => g.Key, g => g.Select(x => x.Price).ToList());

        foreach (var rt in roomTypes)
        {
            if (ratesByRt.TryGetValue(rt.Id, out var prices) && prices.Count > 0)
                nightlyByRoomType[rt.Id] = prices.Min();
            else
                nightlyByRoomType[rt.Id] = rt.BasePrice;
        }

        bool IsRoomTypeAvailable(RoomType rt)
        {
            var nights = (checkOut - checkIn).Days;
            if (nights <= 0) return false;

            if (hasRoomAvailability)
            {

                for (var i = 0; i < nights; i++)
                {
                    var d = checkIn.AddDays(i);
                    var rec = _db.Set<RoomAvailability>()
                        .Local
                        .FirstOrDefault(a => a.RoomTypeId == rt.Id && a.Date == d);
                    if (rec is null || rec.Available <= 0) return false;
                }
                return true;
            }
            else
            {
                var stock = rt.Stock;
                reservationsByRoomType!.TryGetValue(rt.Id, out var overlapsForRt);
                overlapsForRt ??= new List<Reservation>();

                for (var i = 0; i < nights; i++)
                {
                    var dayStart = checkIn.AddDays(i);
                    var dayEnd = dayStart.AddDays(1);

                    var overlappingCount = overlapsForRt.Count(r => r.CheckIn < dayEnd && r.CheckOut > dayStart);
                    if (overlappingCount >= stock) return false;
                }
                return true;
            }
        }


        var items = new List<HotelSearchItemResponse>(capacity: candidateHotels.Count);
        foreach (var h in candidateHotels)
        {
            var rts = roomTypes.Where(rt => rt.HotelId == h.Id).ToList();
            if (rts.Count == 0) continue;


            var eligibleAvailable = rts.Where(IsRoomTypeAvailable).ToList();
            if (eligibleAvailable.Count == 0)
            {
                continue;
            }


            var minNightly = eligibleAvailable
                .Select(rt => nightlyByRoomType[rt.Id])
                .DefaultIfEmpty(0m)
                .Min();

            if (req.MinPrice.HasValue && minNightly < req.MinPrice.Value) continue;
            if (req.MaxPrice.HasValue && minNightly > req.MaxPrice.Value) continue;

            var thumb = h.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => i.Url)
                .FirstOrDefault() ?? string.Empty;

            items.Add(new HotelSearchItemResponse
            {
                Id = h.Id,
                Name = h.Name,
                City = h.City.Name,
                FromPrice = minNightly,
                Rating = (double)h.Rating,
                ThumbnailUrl = thumb,
                HasAvailability = true,
                Tags = tagsLookup.TryGetValue(h.Id, out var list) ? list : new List<TagResponse>()
            });
        }


        items = (req.Sort?.ToLowerInvariant()) switch
        {
            "rating" => items.OrderByDescending(i => i.Rating).ThenBy(i => i.FromPrice).ToList(),
            "price" => items.OrderBy(i => i.FromPrice).ThenByDescending(i => i.Rating).ToList(),
            _ => items.OrderBy(i => i.Name).ToList()
        };


        var total = items.Count;
        var skip = (Math.Max(1, req.Page) - 1) * Math.Max(1, req.PageSize);
        var pageItems = items.Skip(skip).Take(Math.Max(1, req.PageSize)).ToList();

        return new PagedResult<HotelSearchItemResponse>
        {
            Items = pageItems,
            TotalCount = total
        };
    }

    private static PagedResult<HotelSearchItemResponse> Paged(
        List<Hotel> scope, List<HotelSearchItemResponse> items, int page, int pageSize)
        => new()
        {
            Items = new List<HotelSearchItemResponse>(),
            TotalCount = 0
        };
}
