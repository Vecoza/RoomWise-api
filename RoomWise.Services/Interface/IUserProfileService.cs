using RoomWise.Model.Requests;
using RoomWise.Model.Responses;

namespace RoomWise.Services.Interface;

public interface IUserProfileService
{
    Task<UserProfileResponse?> GetMineAsync(string userId, CancellationToken ct = default);
    Task<UserProfileResponse> UpsertMineAsync(string userId, UserProfileUpsertRequest req, CancellationToken ct = default);

    Task<UserProfileResponse> SetAvatarAsync(string userId, string avatarUrl, CancellationToken ct);


}