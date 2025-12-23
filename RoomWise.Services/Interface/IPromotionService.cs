using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;

namespace RoomWise.Services.Interface;

public interface IPromotionService
    : ICRUDService<PromotionResponse, PromotionSearchObject, PromotionUpsertRequest, PromotionUpsertRequest>
{
    Task<(PromotionResponse Promo, decimal DiscountedNightly)?> FindBestForRangeAsync(
        int? hotelId, DateTime checkIn, DateTime checkOut, decimal baseNightly, CancellationToken ct = default);

    Task<PromotionPreviewResponse> PreviewAsync(PromotionPreviewRequest req, CancellationToken ct = default);
    void ForceHotelScope(int hotelId);
}
