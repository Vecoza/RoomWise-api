using RoomWise.Model.Requests;
using RoomWise.Model.Responses;

namespace RoomWise.Services.Interface;

public interface IPaymentMethodService
{
    Task<PagedResult<PaymentMethodResponse>> GetMineAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<PaymentMethodResponse> CreateAsync(
        string userId,
        PaymentMethodUpsertRequest req,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(
        string userId,
        int id,
        CancellationToken ct = default);
}