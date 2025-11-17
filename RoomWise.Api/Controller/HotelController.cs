using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/hotels")]
public class HotelsController 
	: BaseCRUDController<HotelResponse, HotelSearchObject, HotelUpsertRequest, HotelUpsertRequest>
{
	private readonly IHotelService _hotelService;

	public HotelsController(IHotelService hotelService)
		: base(hotelService)
	{
		_hotelService = hotelService;
	}

	
	[HttpGet("")]
	public override Task<PagedResult<HotelResponse>> Get([FromQuery] HotelSearchObject? search = null)
		=> base.Get(search);

	[HttpGet("search")]
	public async Task<PagedResult<HotelSearchItemResponse>> Search([FromQuery] HotelSearchObject search)
	{
		var hotelService = (IHotelService)_service;
		return await hotelService.SearchAsync(search);
	}

	[HttpGet("{id:int}/details")]
	public async Task<ActionResult<HotelDetailsResponse>> Details(
		int id, [FromQuery] DateTime? checkIn, [FromQuery] DateTime? checkOut, [FromQuery] int? guests)
	{
		var hotelService = (IHotelService)_service;
		var dto = await hotelService.GetDetailsAsync(id, checkIn, checkOut, guests);
		if (dto is null) return NotFound();
		return dto;
	}
}
