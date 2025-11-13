using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RoomWise.Services.Services;

public sealed class HotelService
    : BaseCRUDService<HotelResponse, HotelSearchObject, Hotel, HotelUpsertRequest, HotelUpsertRequest>,
        IHotelService
{
    public HotelService(DbContext context, IMapper mapper) : base(context, mapper) { }

    protected override IQueryable<Hotel> ApplyFilter(IQueryable<Hotel> q, HotelSearchObject s)
    {
        if (s.CityId.HasValue) q = q.Where(x => x.CityId == s.CityId.Value);
		if (s.CountryId.HasValue) q = q.Where(x => x.City.CountryId == s.CountryId.Value);
        if (!string.IsNullOrWhiteSpace(s.Name)) q = q.Where(x => x.Name.Contains(s.Name));
		if (!string.IsNullOrWhiteSpace(s.Query)) q = q.Where(x => x.Name.Contains(s.Query!) || x.Description.Contains(s.Query!));
        if (s.MinRating.HasValue) q = q.Where(x => x.Rating >= s.MinRating.Value);
        if (s.MaxRating.HasValue) q = q.Where(x => x.Rating <= s.MaxRating.Value);
		return q.OrderByDescending(x => x.Rating).ThenBy(x => x.Id);
    }

	public async Task<PagedResult<HotelSearchItemResponse>> SearchAsync(HotelSearchObject search)
	{
		var q = _context.Set<Hotel>()
			.Include(h => h.City)
			.Include(h => h.Images)
			.AsQueryable();

		q = ApplyFilter(q, search);

	
		int? total = null;
		if (search.IncludeTotalCount) total = await q.CountAsync();
		if (!search.RetrieveAll)
		{
			if (search.Page.HasValue) q = q.Skip(search.Page.Value * (search.PageSize ?? 10));
			if (search.PageSize.HasValue) q = q.Take(search.PageSize.Value);
		}

		var hotels = await q.ToListAsync();

		var checkIn = search.CheckIn?.Date;
		var checkOut = search.CheckOut?.Date;
		var guests = search.Guests ?? 1;
		var budgetMin = search.BudgetMin;
		var budgetMax = search.BudgetMax;

		bool hasDates = checkIn.HasValue && checkOut.HasValue && checkOut > checkIn;
		int nights = hasDates ? (checkOut!.Value - checkIn!.Value).Days : 0;

		var roomTypes = await _context.Set<RoomType>()
			.Where(rt => hotels.Select(h => h.Id).Contains(rt.HotelId))
			.ToListAsync();

		var hotelIdToRoomTypes = roomTypes.GroupBy(rt => rt.HotelId).ToDictionary(g => g.Key, g => g.ToList());

		List<RoomRate> rates = new();
		if (hasDates || budgetMin.HasValue || budgetMax.HasValue)
		{
			var roomTypeIds = roomTypes.Select(rt => rt.Id).ToList();
			rates = await _context.Set<RoomRate>()
				.Where(rr => roomTypeIds.Contains(rr.RoomTypeId))
				.ToListAsync();
		}

		var hasAvailabilityTable = _context.Model.FindEntityType(typeof(RoomAvailability)) is not null;
		List<RoomAvailability> availabilities = new();
		if (hasAvailabilityTable && hasDates)
		{
			var roomTypeIds = roomTypes.Select(rt => rt.Id).ToList();
			availabilities = await _context.Set<RoomAvailability>()
				.Where(a => roomTypeIds.Contains(a.RoomTypeId)
				            && a.Date >= checkIn!.Value
				            && a.Date < checkOut!.Value)
				.ToListAsync();
		}

		List<Reservation> overlappingReservations = new();
		if (!hasAvailabilityTable && hasDates)
		{
			var roomTypeIds = roomTypes.Select(rt => rt.Id).ToList();
			overlappingReservations = await _context.Set<Reservation>()
				.Where(r => roomTypeIds.Contains(r.RoomTypeId)
				            && r.Status != "Cancelled"
				            && r.CheckIn < checkOut!.Value
				            && checkIn!.Value < r.CheckOut)
				.ToListAsync();
		}

		var results = new List<HotelSearchItemResponse>();
		foreach (var h in hotels)
		{
			var types = hotelIdToRoomTypes.TryGetValue(h.Id, out var list) ? list : new List<RoomType>();

			var eligibleTypes = types.Where(rt => rt.Capacity >= guests).ToList();
			if (eligibleTypes.Count == 0)
			{
				
				if (search.Guests.HasValue) continue;
			}

			
			decimal fromPrice = 0m;
			if (eligibleTypes.Count > 0)
			{
				IEnumerable<RoomRate> rtRates = rates.Where(r => eligibleTypes.Select(et => et.Id).Contains(r.RoomTypeId));
				if (hasDates)
				{
					rtRates = rtRates.Where(r => r.StartDate <= checkIn!.Value && r.EndDate >= checkOut!.Value);
				}
				if (rtRates.Any())
				{
					fromPrice = rtRates.Min(r => r.Price);
				}
				else
				{
					
					fromPrice = eligibleTypes.Min(rt => rt.BasePrice);
				}

				
				if (budgetMin.HasValue && fromPrice < budgetMin.Value) continue;
				if (budgetMax.HasValue && fromPrice > budgetMax.Value) continue;
			}

		
			bool hasAvailability = true;
			if (hasDates && eligibleTypes.Count > 0)
			{
				if (hasAvailabilityTable)
				{
					hasAvailability = eligibleTypes.Any(rt =>
					{
						var dates = Enumerable.Range(0, nights).Select(i => checkIn!.Value.AddDays(i));
						return dates.All(d =>
							availabilities.Any(a => a.RoomTypeId == rt.Id && a.Date == d && a.Available > 0));
					});
				}
				else
				{
					hasAvailability = eligibleTypes.Any(rt =>
					{
						var count = overlappingReservations.Count(r => r.RoomTypeId == rt.Id);
						return rt.Stock - count > 0;
					});
				}
			}

		
			var item = new HotelSearchItemResponse
			{
				Id = h.Id,
				Name = h.Name,
				City = h.City.Name,
				FromPrice = fromPrice,
				Rating = (double)h.Rating,
				ThumbnailUrl = h.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault() ?? string.Empty,
				HasAvailability = hasAvailability
			};
			results.Add(item);
		}

		return new PagedResult<HotelSearchItemResponse>
		{
			Items = results,
			TotalCount = total
		};
	}

	public async Task<HotelDetailsResponse?> GetDetailsAsync(int id, DateTime? checkIn, DateTime? checkOut, int? guests)
	{
		var hotel = await _context.Set<Hotel>()
			.Include(h => h.City)
			.Include(h => h.Images)
			.Include(h => h.HotelFacilities)
				.ThenInclude(hf => hf.Facility)
			.Include(h => h.RoomTypes)
			.FirstOrDefaultAsync(h => h.Id == id);
		if (hotel is null) return null;

		var result = new HotelDetailsResponse
		{
			Id = hotel.Id,
			Name = hotel.Name,
			AddressLine = hotel.AddressLine,
			Description = hotel.Description,
			Rating = (double)hotel.Rating,
			City = hotel.City.Name,
			Amenities = hotel.HotelFacilities.Select(hf => hf.Facility.Name).ToList(),
			Photos = hotel.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList()
		};

		var types = hotel.RoomTypes.ToList();
		var requestedGuests = guests ?? 1;

		var hasDates = checkIn.HasValue && checkOut.HasValue && checkOut > checkIn;
		int nights = hasDates ? (checkOut!.Value.Date - checkIn!.Value.Date).Days : 0;

		var eligibleTypes = types.Where(rt => rt.Capacity >= requestedGuests).ToList();


		List<RoomRate> rates = new();
		if (eligibleTypes.Count > 0)
		{
			var ids = eligibleTypes.Select(rt => rt.Id).ToList();
			rates = await _context.Set<RoomRate>()
				.Where(r => ids.Contains(r.RoomTypeId))
				.ToListAsync();
		}

		var hasAvailabilityTable = _context.Model.FindEntityType(typeof(RoomAvailability)) is not null;
		List<RoomAvailability> availabilities = new();
		if (hasAvailabilityTable && hasDates && eligibleTypes.Count > 0)
		{
			var ids = eligibleTypes.Select(rt => rt.Id).ToList();
			availabilities = await _context.Set<RoomAvailability>()
				.Where(a => ids.Contains(a.RoomTypeId)
				            && a.Date >= checkIn!.Value.Date
				            && a.Date < checkOut!.Value.Date)
				.ToListAsync();
		}

		List<Reservation> overlappingReservations = new();
		if (!hasAvailabilityTable && hasDates && eligibleTypes.Count > 0)
		{
			var ids = eligibleTypes.Select(rt => rt.Id).ToList();
			overlappingReservations = await _context.Set<Reservation>()
				.Where(r => ids.Contains(r.RoomTypeId)
				            && r.Status != "Cancelled"
				            && r.CheckIn < checkOut!.Value
				            && checkIn!.Value < r.CheckOut)
				.ToListAsync();
		}

		var details = new List<AvailableRoomType>();
		foreach (var rt in eligibleTypes)
		{
			decimal nightly = rt.BasePrice;
			if (rates.Count > 0)
			{
				var rtRates = rates.Where(r => r.RoomTypeId == rt.Id);
				if (hasDates)
					rtRates = rtRates.Where(r => r.StartDate <= checkIn!.Value.Date && r.EndDate >= checkOut!.Value.Date);
				if (rtRates.Any()) nightly = rtRates.Min(r => r.Price);
			}

			int roomsLeft = rt.Stock;
			if (hasDates)
			{
				if (hasAvailabilityTable)
				{
					var dates = Enumerable.Range(0, nights).Select(i => checkIn!.Value.Date.AddDays(i));
					roomsLeft = dates.Select(d => availabilities.FirstOrDefault(a => a.RoomTypeId == rt.Id && a.Date == d)?.Available ?? 0)
						.Min();
				}
				else
				{
					var count = overlappingReservations.Count(r => r.RoomTypeId == rt.Id);
					roomsLeft = Math.Max(0, rt.Stock - count);
				}
			}

			details.Add(new AvailableRoomType
			{
				RoomTypeId = rt.Id,
				Name = rt.Name,
				Capacity = rt.Capacity,
				NightlyPrice = nightly,
				RoomsLeft = roomsLeft
			});
		}

		result.AvailableRoomTypes = details;
		return result;
    }
}