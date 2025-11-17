// RoomWise.Services/Services/ReviewService.cs
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class ReviewService
    : BaseCRUDService<ReviewResponse, ReviewSearchObject, Review, ReviewUpsertRequest, ReviewUpsertRequest>, IReviewService
{
    private readonly DbContext _ctx;

    public ReviewService(DbContext ctx, IMapper mapper) : base(ctx, mapper)
    {
        _ctx = ctx;
    }

    protected override IQueryable<Review> ApplyFilter(IQueryable<Review> q, ReviewSearchObject s)
    {
        if (s.HotelId.HasValue) q = q.Where(x => x.HotelId == s.HotelId.Value);
        if (!string.IsNullOrWhiteSpace(s.UserId)) q = q.Where(x => x.UserId == s.UserId);
        if (s.MinRating.HasValue) q = q.Where(x => x.Rating >= s.MinRating.Value);
        if (s.MaxRating.HasValue) q = q.Where(x => x.Rating <= s.MaxRating.Value);
        if (!string.IsNullOrWhiteSpace(s.FTS))
            q = q.Where(x =>
                (x.Title != null && EF.Functions.ILike(x.Title, $"%{s.FTS}%")) ||
                (x.Body  != null && EF.Functions.ILike(x.Body,  $"%{s.FTS}%")));
        return q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id);
    }

    protected override async Task BeforeInsert(Review entity, ReviewUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UserId))
            throw new InvalidOperationException("User context missing.");

        var today = DateTime.UtcNow.Date;

        var hasStayed = await _ctx.Set<Reservation>()
            .AnyAsync(r =>
                r.UserId == req.UserId &&
                r.HotelId == req.HotelId &&
                (r.Status == "Confirmed" || r.Status == "Completed") &&
                r.CheckOut.Date <= today);

        if (!hasStayed)
            throw new InvalidOperationException("You can review this hotel only after a completed stay.");

        var already = await _ctx.Set<Review>()
            .AnyAsync(r => r.HotelId == req.HotelId && r.UserId == req.UserId);

        if (already)
            throw new InvalidOperationException("You have already reviewed this hotel.");

        entity.UserId = req.UserId!;
        entity.CreatedAt = DateTime.UtcNow;
    }

    public async Task<ReviewResponse> CreateAsync(ReviewUpsertRequest req, CancellationToken ct = default)
    {
        using var tx = await _ctx.Database.BeginTransactionAsync(ct);

 
        var entity = _mapper.Map<Review>(req);

        await BeforeInsert(entity, req);

        _ctx.Set<Review>().Add(entity);
        await _ctx.SaveChangesAsync(ct);

        var hotel = await _ctx.Set<Hotel>().FirstOrDefaultAsync(h => h.Id == req.HotelId, ct)
                    ?? throw new InvalidOperationException("Hotel not found.");

        var count  = hotel.ReviewCount;          
        var oldAvg = (double)hotel.Rating;        
        var newAvg = (oldAvg * count + req.Rating) / (count + 1);

        hotel.ReviewCount = count + 1;
        hotel.Rating = (decimal)Math.Round(newAvg, 2);

        await _ctx.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return MapToResponse(entity);
    }

    public async Task<PagedResult<ReviewResponse>> ListByHotelAsync(
        int hotelId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var q = _ctx.Set<Review>()
            .Where(r => r.HotelId == hotelId)
            .OrderByDescending(r => r.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((Math.Max(1, page) - 1) * Math.Max(1, pageSize))
                           .Take(Math.Max(1, pageSize))
                           .ToListAsync(ct);

        return new PagedResult<ReviewResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = total
        };
    }
}
