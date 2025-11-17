using RoomWise.Model.Responses;

namespace RoomWise.Services.Interface;

public interface IReportService
{
    Task<ReservationSummaryResponse> GetReservationSummaryAsync(
        ReservationReportFilter filter,
        CancellationToken ct = default);
}