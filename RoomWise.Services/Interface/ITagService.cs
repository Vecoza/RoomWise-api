using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;

namespace RoomWise.Services.Interface;

public interface ITagService
    : ICRUDService<TagResponse, TagSearchObject, TagUpsertRequest, TagUpsertRequest>
{
    Task SetForHotelAsync(int hotelId, IEnumerable<int> tagIds, CancellationToken ct = default);
}