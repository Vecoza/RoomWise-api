using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;


[ApiController]
[Route("api/[controller]")]
public class HotelController : BaseCRUDController<HotelResponse, HotelSearchObject, HotelUpsertRequest, HotelUpsertRequest>
{
	public HotelController(IHotelService svc) : base(svc) { }

	[HttpGet("/api/hotels")]
	public async Task<PagedResult<HotelSearchItemResponse>> Search([FromQuery] HotelSearchObject search)
	{
		if (_service is IHotelService hotelService)
		{
			return await hotelService.SearchAsync(search);
		}
		// Fallback should never happen; keep signature stable
		return new PagedResult<HotelSearchItemResponse> { Items = new List<HotelSearchItemResponse>(), TotalCount = 0 };
	}

	[HttpGet("/api/hotels/{id:int}")]
	public async Task<ActionResult<HotelDetailsResponse>> Details([FromRoute] int id, [FromQuery] DateTime? checkIn, [FromQuery] DateTime? checkOut, [FromQuery] int? guests)
	{
		if (_service is not IHotelService hotelService) return NotFound();
		var dto = await hotelService.GetDetailsAsync(id, checkIn, checkOut, guests);
		if (dto is null) return NotFound();
		return dto;
	}
}