using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class WishlistService : IWishlistService
{
    private readonly DbContext _context;

    public WishlistService(DbContext context) => _context = context;

    public async Task<bool> AddAsync(string userId, int hotelId)
    {

        var userIdStr = userId;


        var hotelExists = await _context.Set<Hotel>()
            .AsNoTracking()
            .AnyAsync(h => h.Id == hotelId);

        if (!hotelExists) return false;

        var already = await _context.Set<Wishlist>()
            .AnyAsync(w => w.UserId == userIdStr && w.HotelId == hotelId);

        if (already) return true;


        _context.Set<Wishlist>().Add(new Wishlist
        {
            UserId = userIdStr,
            HotelId = hotelId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveAsync(string userId, int hotelId)
    {
        var userIdStr = userId;

        var entity = await _context.Set<Wishlist>()
            .FirstOrDefaultAsync(w => w.UserId == userIdStr && w.HotelId == hotelId);

        if (entity is null) return true; // idempotent

        _context.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<WishlistResponse>> ListAsync(string userId)
    {
        var userIdStr = userId;

        var query =
            from w in _context.Set<Wishlist>().AsNoTracking()
            where w.UserId == userIdStr
            join h in _context.Set<Hotel>().AsNoTracking() on w.HotelId equals h.Id
            select new
            {
                WishlistId = w.Id,
                w.UserId,
                w.HotelId,
                w.CreatedAt,
                h.Name,
                CityName = h.City.Name,
                ThumbnailUrl = _context.Set<HotelImage>()
                    .Where(img => img.HotelId == h.Id)
                    .OrderBy(img => img.SortOrder)
                    .Select(img => img.Url)
                    .FirstOrDefault(),
                FromPrice =
                    (decimal?)_context.Set<RoomRate>()
                        .Where(r => r.RoomType.HotelId == h.Id)
                        .Select(r => (decimal?)r.Price)
                        .Min()
                    ?? _context.Set<RoomType>()
                        .Where(rt => rt.HotelId == h.Id)
                        .Select(rt => (decimal?)rt.BasePrice)
                        .Min(),
                Rating = (double?)EF.Property<decimal?>(h, "Rating") ?? 0.0
            };

        var list = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new WishlistResponse
            {
                Id = x.WishlistId,
                UserId = x.UserId,
                HotelId = x.HotelId,
                CreatedAt = x.CreatedAt,
                Hotel = new HotelSearchItemResponse
                {
                    Id = x.HotelId,
                    Name = x.Name,
                    City = x.CityName ?? string.Empty,
                    FromPrice = x.FromPrice ?? 0m,
                    Rating = x.Rating,
                    ThumbnailUrl = x.ThumbnailUrl ?? string.Empty,
                    HasAvailability = true,
                    Tags = _context.Set<HotelTag>()
                        .Where(ht => ht.HotelId == x.HotelId)
                        .Include(ht => ht.Tag)
                        .Select(ht => new TagResponse { Id = ht.TagId, Name = ht.Tag.Name })
                        .ToList()
                }
            })
            .ToListAsync();

        return list;
    }
}
