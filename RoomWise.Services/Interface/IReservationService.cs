using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;

namespace RoomWise.Services.Interface
{
    public interface IReservationService
        : ICRUDService<ReservationResponse, ReservationSearchObject, ReservationUpsertRequest, ReservationUpsertRequest>
    {
        Task<bool> CancelAsync(Guid id, Guid requestedByUserId);
        Task<PagedResult<ReservationResponse>> GetMyAsync(Guid userId, string? category);

        
        Task<ReservationResponse> InsertAsync(ReservationUpsertRequest request);
        Task<ReservationResponse?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default);

        Task<(PaymentResponse Payment, string ClientSecret)?> FindActivePaymentAsync(int reservationId);
    }
}