
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class UserProfileService : IUserProfileService
{
    private readonly DbContext _db;
    private readonly IMapper _mapper;

    public UserProfileService(DbContext db, IMapper mapper)
    {
        _db = db; _mapper = mapper;
    }

    public async Task<UserProfileResponse?> GetMineAsync(string userId, CancellationToken ct = default)
    {
        var entity = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        return entity is null ? null : _mapper.Map<UserProfileResponse>(entity);
    }

    public async Task<UserProfileResponse> UpsertMineAsync(string userId, UserProfileUpsertRequest req, CancellationToken ct = default)
    {
        var entity = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (entity is null)
        {
            entity = new UserProfile
            {
                UserId = userId,
                FirstName = req.FirstName,
                LastName  = req.LastName,
                Phone     = req.Phone,
                AvatarUrl = req.AvatarUrl,
                PreferredLanguage = req.PreferredLanguage,
                LoyaltyBalance = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Set<UserProfile>().Add(entity);
        }
        else
        {
            entity.FirstName = req.FirstName;
            entity.LastName  = req.LastName;
            entity.Phone     = req.Phone;
            entity.AvatarUrl = req.AvatarUrl;
            entity.PreferredLanguage = req.PreferredLanguage;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return _mapper.Map<UserProfileResponse>(entity);
    }
}
