using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;


public class PromotionService
    : BaseCRUDService<PromotionResponse, PromotionSearchObject, Promotion, PromotionUpsertRequest,
            PromotionUpsertRequest>,
        IPromotionService
{
    private readonly DbContext _db;

    public PromotionService(DbContext db, IMapper mapper) : base(db, mapper)
    {
        _db = db;
    }

    protected override IQueryable<Promotion> ApplyFilter(IQueryable<Promotion> q, PromotionSearchObject s)
    {
        if (s.HotelId.HasValue) q = q.Where(x => x.HotelId == s.HotelId.Value);
        if (s.ActiveOnly == true) q = q.Where(x => x.IsActive);
        if (s.MinNightsGte.HasValue) q = q.Where(x => x.MinNights >= s.MinNightsGte.Value);

        if (s.From.HasValue && s.To.HasValue)
        {
            var from = s.From.Value.Date;
            var to = s.To.Value.Date;
            // overlap
            q = q.Where(x => x.EndDate >= from && x.StartDate <= to);
        }

        if (!string.IsNullOrWhiteSpace(s.FTS))
            q = q.Where(x => EF.Functions.ILike(x.Title, $"%{s.FTS}%") ||
                             (x.Description != null && EF.Functions.ILike(x.Description, $"%{s.FTS}%")));

        return q.OrderByDescending(x => x.IsActive).ThenBy(x => x.StartDate);
    }

    protected override Task BeforeInsert(Promotion entity, PromotionUpsertRequest req)
        => ValidateAndNormalize(entity, req);

    protected override Task BeforeUpdate(Promotion entity, PromotionUpsertRequest req)
        => ValidateAndNormalize(entity, req);

    private static Task ValidateAndNormalize(Promotion entity, PromotionUpsertRequest req)
    {
        var from = req.StartDate.Date;
        var to = req.EndDate.Date;
        if (to < from) throw new ArgumentException("EndDate must be on/after StartDate.");

        var hasPct = req.DiscountPercent is not null && req.DiscountPercent.Value > 0;
        var hasFix = req.DiscountFixed is not null && req.DiscountFixed.Value > 0;
        if (hasPct == hasFix) 
            throw new ArgumentException("Provide either DiscountPercent or DiscountFixed (not both).");

        if (req.DiscountPercent is { } p && (p < 0 || p > 100))
            throw new ArgumentException("DiscountPercent must be between 0 and 100.");

        entity.StartDate = from;
        entity.EndDate = to;
        return Task.CompletedTask;
    }

    public async Task<(PromotionResponse Promo, decimal DiscountedNightly)?> FindBestForRangeAsync(
        int? hotelId, DateTime checkIn, DateTime checkOut, decimal baseNightly, CancellationToken ct = default)
    {
        var from = checkIn.Date;
        var toIncl = checkOut.Date.AddDays(-1);
        var nights = (checkOut.Date - checkIn.Date).Days;
        if (nights <= 0 || baseNightly <= 0) return null;

        var q = _db.Set<Promotion>()
            .Where(p => p.IsActive
                        && p.StartDate <= from
                        && p.EndDate >= toIncl
                        && p.MinNights <= nights);

         if (hotelId.HasValue)
            q = q.Where(p => p.HotelId == hotelId.Value || p.HotelId == null);
        else
            q = q.Where(p => p.HotelId == null);

        var promos = await q.ToListAsync(ct);
        if (promos.Count == 0) return null;

        Promotion? best = null;
        decimal bestNightly = baseNightly;

        foreach (var p in promos)
        {
            var discounted = p.DiscountPercent is { } pct && pct > 0
                ? Math.Max(0m, baseNightly * (1m - pct / 100m))
                : Math.Max(0m, baseNightly - (p.DiscountFixed ?? 0m));

            if (best == null || discounted < bestNightly)
            {
                best = p;
                bestNightly = discounted;
            }
            else if (discounted == bestNightly)
            {
                int Rank(Promotion x) => x.HotelId.HasValue ? 2 : 1;
                if (Rank(p) > Rank(best) || (Rank(p) == Rank(best) && p.StartDate > best.StartDate))
                {
                    best = p;
                    bestNightly = discounted;
                }
            }
        }

        return (_mapper.Map<PromotionResponse>(best!), bestNightly);
    }

    public async Task<PromotionPreviewResponse> PreviewAsync(PromotionPreviewRequest req,
        CancellationToken ct = default)
    {
        var nights = (req.CheckOut.Date - req.CheckIn.Date).Days;
        if (nights <= 0) throw new ArgumentException("CheckOut must be after CheckIn.");

        var totalBefore = req.BaseNightly * nights;
        var best = await FindBestForRangeAsync(req.HotelId, req.CheckIn, req.CheckOut, req.BaseNightly, ct);

        if (best is null)
            return new PromotionPreviewResponse
            {
                DiscountedNightly = req.BaseNightly,
                TotalBefore = totalBefore,
                TotalAfter = totalBefore
            };

        var (promo, discountedNightly) = best.Value;
        return new PromotionPreviewResponse
        {
            PromotionId = promo.Id,
            Title = promo.Title,
            DiscountedNightly = discountedNightly,
            TotalBefore = totalBefore,
            TotalAfter = discountedNightly * nights
        };
    }
}