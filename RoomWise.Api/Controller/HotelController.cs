using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;
using RoomWise.Api.Auth;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/hotels")]
/*[Authorize(Roles = AppRoles.Administrator)]*/
public class HotelsController
	: BaseCRUDController<HotelResponse, HotelSearchObject, HotelUpsertRequest, HotelUpsertRequest>
{
	private readonly IHotelService _hotelService;
    private readonly HotelAdminScope _scope;

    public HotelsController(IHotelService hotelService, HotelAdminScope scope)
        : base(hotelService)
    {
        _hotelService = hotelService;
        _scope = scope;
	}


	[AllowAnonymous]
	[HttpGet("")]
	public override Task<PagedResult<HotelResponse>> Get([FromQuery] HotelSearchObject? search = null)
		=> base.Get(search);

	[AllowAnonymous]
	[HttpGet("search")]
	public async Task<PagedResult<HotelSearchItemResponse>> Search([FromQuery] HotelSearchObject search)
	{
		var hotelService = (IHotelService)_service;
		return await hotelService.SearchAsync(search);
	}

	[AllowAnonymous]
	[HttpGet("{id:int}/details")]
	public async Task<ActionResult<HotelDetailsResponse>> Details(
		int id, [FromQuery] DateTime? checkIn, [FromQuery] DateTime? checkOut, [FromQuery] int? guests)
	{
		var hotelService = (IHotelService)_service;
		var dto = await hotelService.GetDetailsAsync(id, checkIn, checkOut, guests);
		if (dto is null) return NotFound();
		return dto;
	}

	[AllowAnonymous]
	[HttpGet("hot-deals")]
	public async Task<PagedResult<HotelSearchItemResponse>> HotDeals([FromQuery] int page = 0, [FromQuery] int pageSize = 20, CancellationToken ct = default)
	{
		var hotelService = (IHotelService)_service;
		return await hotelService.GetHotDealsAsync(page, pageSize, ct);
	}

    [Authorize(Roles = AppRoles.Administrator)]
    [HttpGet("{id:int}")]
    public override async Task<HotelResponse?> GetById(int id)
    {
        var hotelId = await _scope.GetHotelIdAsync();
        if (hotelId.HasValue && hotelId.Value != id) return null;
        return await base.GetById(id);
    }

    [Authorize(Roles = AppRoles.Administrator)]
    [HttpPut("{id:int}")]
    public override async Task<HotelResponse?> Update(int id, [FromBody] HotelUpsertRequest req)
    {
        var hotelId = await _scope.GetHotelIdAsync();
        if (hotelId.HasValue && hotelId.Value != id) return null;
        return await base.Update(id, req);
    }
}
