using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;

namespace RoomWise.Services.Interface
{
    public interface IReservationService
        : ICRUDService<ReservationResponse, ReservationSearchObject, ReservationUpsertRequest, ReservationUpsertRequest>
    {
        Task CancelAsync(int id, CancellationToken ct);

        Task<PagedResult<ReservationResponse>> GetMyAsync(string userId, string? category);


        new Task<ReservationResponse> CreateAsync(ReservationUpsertRequest request);
        Task<ReservationResponse?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default);

        Task<(PaymentResponse Payment, string ClientSecret)?> FindActivePaymentAsync(int reservationId);
        void ForceHotelScope(int hotelId);
        Task CancelAsAdminAsync(int id, CancellationToken ct);
    }
}
