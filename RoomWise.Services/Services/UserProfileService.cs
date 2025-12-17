using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
        _db = db;
        _mapper = mapper;
    }

    public async Task<UserProfileResponse?> GetMineAsync(string userId, CancellationToken ct = default)
    {
        var entity = await _db.Set<UserProfile>()
                              .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        return entity is null ? null : _mapper.Map<UserProfileResponse>(entity);
    }

    public async Task<UserProfileResponse> UpsertMineAsync(
        string userId,
        UserProfileUpsertRequest req,
        CancellationToken ct = default)
    {
        // ✅ 1) Make sure the identity user actually exists
        var userExists = await _db.Set<AppUser>()
                                  .AnyAsync(u => u.Id == userId, ct);

        if (!userExists)
        {
            // This avoids FK violations on UserProfiles.UserId
            throw new InvalidOperationException(
                $"Cannot create or update profile. User '{userId}' does not exist.");
        }

        // ✅ 2) Normal upsert logic
        var entity = await _db.Set<UserProfile>()
                              .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (entity is null)
        {
            entity = new UserProfile
            {
                UserId = userId,
                FirstName = req.FirstName,
                LastName = req.LastName,
                Phone = req.Phone,
                AvatarUrl = req.AvatarUrl,
                PreferredLanguage = req.PreferredLanguage,
                LoyaltyBalance = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Set<UserProfile>().Add(entity); // may race with a parallel request
        }
        else
        {
            entity.FirstName = req.FirstName;
            entity.LastName = req.LastName;
            entity.Phone = req.Phone;
            entity.AvatarUrl = req.AvatarUrl;
            entity.PreferredLanguage = req.PreferredLanguage;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Handle a rare race where another request created the profile first.
            var existing = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (existing is null) throw;
            entity = existing;
        }

        return _mapper.Map<UserProfileResponse>(entity);
    }


    public async Task<UserProfileResponse> SetAvatarAsync(
    string userId,
    string avatarBase64,
    CancellationToken ct)
    {
        var profile = await _db.Set<UserProfile>()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

        if (profile == null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                FirstName = "",
                LastName = "",
                CreatedAt = DateTime.UtcNow
            };
            _db.Set<UserProfile>().Add(profile);
        }


        profile.AvatarUrl = avatarBase64;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new UserProfileResponse
        {
            UserId = profile.UserId,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Phone = profile.Phone,
            PreferredLanguage = profile.PreferredLanguage,
            AvatarUrl = profile.AvatarUrl,
            LoyaltyBalance = profile.LoyaltyBalance,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }

}
