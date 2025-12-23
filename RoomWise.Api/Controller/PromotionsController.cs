using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using RoomWise.Model;
using RoomWise.Api.Auth;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]

public sealed class PromotionsController
    : BaseCRUDController<PromotionResponse, PromotionSearchObject, PromotionUpsertRequest, PromotionUpsertRequest>
{
    private readonly IPromotionService _promos;
    private readonly HotelAdminScope _scope;

    public PromotionsController(IPromotionService promos, HotelAdminScope scope) : base(promos)
    {
        _promos = promos;
        _scope = scope;
    }

    [HttpPost("preview")]

    public async Task<ActionResult<PromotionPreviewResponse>> Preview([FromBody] PromotionPreviewRequest req, CancellationToken ct)
        => Ok(await _promos.PreviewAsync(req, ct));

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<PagedResult<PromotionResponse>> Get([FromQuery] PromotionSearchObject? search = null)
    {
        return Filtered(async () => await base.Get(search));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<PromotionResponse> Create([FromBody] PromotionUpsertRequest req)
    {
        return Filtered(async () => await base.Create(req));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<PromotionResponse?> Update(int id, [FromBody] PromotionUpsertRequest req)
    {
        return Filtered(async () => await base.Update(id, req));
    }

    private async Task<T> Filtered<T>(Func<Task<T>> action)
    {
        var hotelId = await _scope.GetHotelIdAsync();
        if (hotelId.HasValue) _promos.ForceHotelScope(hotelId.Value);
        return await action();
    }
}
