using RoomWise.Model.Requests;
using RoomWise.Model.Responses;

namespace RoomWise.Services.Interface;

public interface INotificationService
{
    Task<NotificationResponse> CreateAsync(
        NotificationCreateRequest request,
        CancellationToken ct = default);

    Task<PagedResult<NotificationResponse>> GetForUserAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task MarkAsReadAsync(
        int id,
        string userId,
        CancellationToken ct = default);
}