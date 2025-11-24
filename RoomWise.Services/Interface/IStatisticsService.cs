using RoomWise.Model.Responses;

namespace RoomWise.Services.Interface;

public interface IStatisticsService
{
    Task<AdminStatsOverviewResponse> GetOverviewAsync(CancellationToken ct = default);

    Task<IReadOnlyList<RevenueByMonthItem>> GetRevenueByMonthAsync(
        int year,
        CancellationToken ct = default);

    Task<IReadOnlyList<HotelStatsItem>> GetTopHotelsAsync(
        int limit,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<UserStatsItem>> GetTopUsersAsync(
        int limit,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);
}