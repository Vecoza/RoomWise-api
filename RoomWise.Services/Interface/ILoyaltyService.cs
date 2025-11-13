using RoomWise.Model.Responses;

namespace RoomWise.Services.Interface;

public interface ILoyaltyService
{
    Task AddAsync(string userId, int delta, string reason, int? reservationId = null, CancellationToken ct = default);

    Task<int> GetBalanceAsync(string userId, CancellationToken ct = default);
    Task<PagedResult<LoyaltyPointResponse>> GetHistoryAsync(string userId, int page = 1, int pageSize = 20, CancellationToken ct = default);
}