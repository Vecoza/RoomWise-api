
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class LoyaltyService : ILoyaltyService
{
    private readonly DbContext _db;
    private readonly IMapper _mapper;

    public LoyaltyService(DbContext db, IMapper mapper)
    {
        _db = db; _mapper = mapper;
    }

    public async Task AddAsync(string userId, int delta, string reason, int? reservationId = null, CancellationToken ct = default)
    {
        using var tx = await _db.Database.BeginTransactionAsync(ct);


        _db.Set<LoyaltyPoint>().Add(new LoyaltyPoint
        {
            UserId = userId,
            Delta = delta,
            Reason = reason,
            ReservationId = reservationId,
            CreatedAt = DateTime.UtcNow
        });

        var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                FirstName = "",
                LastName = "",
                PreferredLanguage = "en",
                LoyaltyBalance = delta,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Set<UserProfile>().Add(profile);
        }
        else
        {
            profile.LoyaltyBalance += delta;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<int> GetBalanceAsync(string userId, CancellationToken ct = default)
    {
        var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        return profile?.LoyaltyBalance ?? 0;
    }

    public async Task<PagedResult<LoyaltyPointResponse>> GetHistoryAsync(string userId, int page = 0, int pageSize = 20, CancellationToken ct = default)
    {
        var q = _db.Set<LoyaltyPoint>()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id);

        var total = await q.CountAsync(ct);
        var safePage = Math.Max(0, page);
        var safeSize = Math.Max(1, pageSize);
        var items = await q.Skip(safePage * safeSize)
                           .Take(safeSize)
                           .ToListAsync(ct);

        return new PagedResult<LoyaltyPointResponse>
        {
            Items = items.Select(_mapper.Map<LoyaltyPointResponse>).ToList(),
            TotalCount = total
        };
    }
}
