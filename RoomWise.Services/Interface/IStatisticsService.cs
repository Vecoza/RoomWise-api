using RoomWise.Model.Responses;

namespace RoomWise.Services.Interface;

public interface IStatisticsService
{
    Task<AdminStatsOverviewResponse> GetOverviewAsync(int? hotelId = null, CancellationToken ct = default);

    Task<IReadOnlyList<RevenueByMonthItem>> GetRevenueByMonthAsync(
        int year,
        int? hotelId = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<HotelStatsItem>> GetTopHotelsAsync(
        int limit,
        DateTime? from = null,
        DateTime? to = null,
        int? hotelId = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<UserStatsItem>> GetTopUsersAsync(
        int limit,
        DateTime? from = null,
        DateTime? to = null,
        int? hotelId = null,
        CancellationToken ct = default);
}
