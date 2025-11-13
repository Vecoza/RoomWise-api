using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]

public sealed class PromotionsController
    : BaseCRUDController<PromotionResponse, PromotionSearchObject, PromotionUpsertRequest, PromotionUpsertRequest>
{
    private readonly IPromotionService _promos;

    public PromotionsController(IPromotionService promos) : base(promos) => _promos = promos;

    [HttpPost("preview")]

    public async Task<ActionResult<PromotionPreviewResponse>> Preview([FromBody] PromotionPreviewRequest req, CancellationToken ct)
        => Ok(await _promos.PreviewAsync(req, ct));
}