using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class PaymentMethodService : IPaymentMethodService
{
    private readonly DbContext _db;

    public PaymentMethodService(DbContext db) => _db = db;

    public async Task<PagedResult<PaymentMethodResponse>> GetMineAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        var q = _db.Set<PaymentMethod>()
            .Where(pm => pm.UserId == userId)
            .OrderByDescending(pm => pm.IsDefault)
            .ThenByDescending(pm => pm.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<PaymentMethodResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            TotalCount = total
        };
    }

    public async Task<PaymentMethodResponse> CreateAsync(
        string userId,
        PaymentMethodUpsertRequest req,
        CancellationToken ct = default)
    {

        req.UserId = userId;


        if (req.IsDefault)
        {
            var existingDefaults = await _db.Set<PaymentMethod>()
                .Where(pm => pm.UserId == userId && pm.IsDefault)
                .ToListAsync(ct);

            foreach (var pm in existingDefaults)
                pm.IsDefault = false;
        }

        var entity = new PaymentMethod
        {
            UserId = userId,
            StripePaymentMethodId = req.StripePaymentMethodId,
            Brand = req.Brand,
            Last4 = req.Last4,
            ExpMonth = req.ExpMonth,
            ExpYear = req.ExpYear,
            IsDefault = req.IsDefault,
            CreatedAt = DateTime.UtcNow
        };

        _db.Set<PaymentMethod>().Add(entity);
        await _db.SaveChangesAsync(ct);

        return ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(
        string userId,
        int id,
        CancellationToken ct = default)
    {
        var entity = await _db.Set<PaymentMethod>()
            .FirstOrDefaultAsync(pm => pm.Id == id && pm.UserId == userId, ct);

        if (entity is null) return false;

        _db.Set<PaymentMethod>().Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static PaymentMethodResponse ToResponse(PaymentMethod pm) => new()
    {
        Id = pm.Id,
        UserId = pm.UserId,
        StripePaymentMethodId = pm.StripePaymentMethodId,
        Brand = pm.Brand,
        Last4 = pm.Last4,
        ExpMonth = pm.ExpMonth,
        ExpYear = pm.ExpYear,
        IsDefault = pm.IsDefault,
        CreatedAt = pm.CreatedAt
    };
}
