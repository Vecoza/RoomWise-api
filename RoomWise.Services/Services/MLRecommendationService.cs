using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using RoomWise.Model;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public sealed class MLRecommendationService : IRecommendationService
{
    private readonly DbContext _db;
    private readonly ILogger<MLRecommendationService> _logger;
    private readonly MLContext _ml;

    public MLRecommendationService(DbContext db, ILogger<MLRecommendationService> logger)
    {
        _db = db;
        _logger = logger;
        _ml = new MLContext(seed: 42);
    }

    private sealed class HotelFeatureRow
    {
        public int HotelId { get; set; }
        public string Text { get; set; } = string.Empty;
        public float Rating { get; set; }
        public float Price { get; set; }
    }

    private sealed class HotelFeatureVector
    {
        public int HotelId { get; set; }
        [VectorType]
        public float[] Features { get; set; } = Array.Empty<float>();
    }

    public async Task<IReadOnlyList<HotelSearchItemResponse>> GetForUserAsync(string userId, int top = 10, CancellationToken ct = default)
    {
        top = Math.Max(1, top);


        var interactedIds = await _db.Set<Wishlist>().Where(w => w.UserId == userId).Select(w => w.HotelId)
            .Concat(_db.Set<Reservation>().Where(r => r.UserId == userId).Select(r => r.HotelId))
            .Concat(_db.Set<Review>().Where(rv => rv.UserId == userId).Select(rv => rv.HotelId))
            .Distinct()
            .ToListAsync(ct);

        var hotels = await _db.Set<Hotel>()
            .Include(h => h.City)
            .Include(h => h.Images)
            .Include(h => h.RoomTypes)
            .Include(h => h.HotelTags).ThenInclude(ht => ht.Tag)
            .AsNoTracking()
            .ToListAsync(ct);

        if (hotels.Count == 0) return Array.Empty<HotelSearchItemResponse>();

        var rows = new List<HotelFeatureRow>(hotels.Count);
        foreach (var h in hotels)
        {
            var cheapest = h.RoomTypes.OrderBy(rt => rt.BasePrice).FirstOrDefault()?.BasePrice ?? 0m;
            var tagText = string.Join(' ', h.HotelTags.Select(ht => ht.Tag.Name));
            var text = $"{h.Name} {h.City.Name} {h.Description} {tagText}";

            rows.Add(new HotelFeatureRow
            {
                HotelId = h.Id,
                Text = text,
                Rating = (float)h.Rating,
                Price = (float)cheapest
            });
        }

        var data = _ml.Data.LoadFromEnumerable(rows);
        var pipeline = _ml.Transforms.Text.FeaturizeText("TextFeats", nameof(HotelFeatureRow.Text))
            .Append(_ml.Transforms.NormalizeMeanVariance("RatingNorm", nameof(HotelFeatureRow.Rating)))
            .Append(_ml.Transforms.NormalizeMeanVariance("PriceNorm", nameof(HotelFeatureRow.Price)))
            .Append(_ml.Transforms.Concatenate("Features", "TextFeats", "RatingNorm", "PriceNorm"))
            .Append(_ml.Transforms.NormalizeLpNorm("Features"));

        var model = pipeline.Fit(data);
        var transformed = model.Transform(data);
        var vectors = _ml.Data.CreateEnumerable<HotelFeatureVector>(transformed, reuseRowObject: false).ToList();

        var hotelVectorMap = vectors.ToDictionary(v => v.HotelId, v => v.Features);

        if (interactedIds.Count == 0)
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);

            var bookingCounts = await _db.Set<Reservation>()
                .AsNoTracking()
                .Where(r => r.CreatedAt >= cutoff)
                .GroupBy(r => r.HotelId)
                .Select(g => new { HotelId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var ratingAverages = await _db.Set<Review>()
                .AsNoTracking()
                .Where(rv => rv.CreatedAt >= cutoff)
                .GroupBy(rv => rv.HotelId)
                .Select(g => new { HotelId = g.Key, Avg = g.Average(x => (double)x.Rating) })
                .ToListAsync(ct);

            if (bookingCounts.Count > 0 || ratingAverages.Count > 0)
            {
                var bookingMap = bookingCounts.ToDictionary(x => x.HotelId, x => x.Count);
                var ratingMap = ratingAverages.ToDictionary(x => x.HotelId, x => x.Avg);

                return hotels
                    .Select(h =>
                    {
                        var cheapest = h.RoomTypes.OrderBy(rt => rt.BasePrice).FirstOrDefault()?.BasePrice ?? decimal.MaxValue;
                        bookingMap.TryGetValue(h.Id, out var bookings);
                        ratingMap.TryGetValue(h.Id, out var avgRating);
                        return new { hotel = h, bookings, avgRating, cheapest };
                    })
                    .OrderByDescending(x => x.bookings)
                    .ThenByDescending(x => x.avgRating)
                    .ThenBy(x => x.cheapest)
                    .Take(top)
                    .Select(x => x.hotel)
                    .Select(ToResponse)
                    .ToList();
            }

            return hotels
                .OrderByDescending(h => h.Rating)
                .ThenBy(h => h.RoomTypes.OrderBy(rt => rt.BasePrice).FirstOrDefault()?.BasePrice ?? decimal.MaxValue)
                .Take(top)
                .Select(ToResponse)
                .ToList();
        }


        var dim = hotelVectorMap.Values.FirstOrDefault()?.Length ?? 0;
        if (dim == 0) return Array.Empty<HotelSearchItemResponse>();

        var userVec = new float[dim];
        var count = 0;
        foreach (var id in interactedIds)
        {
            if (hotelVectorMap.TryGetValue(id, out var vec))
            {
                for (int i = 0; i < dim; i++) userVec[i] += vec[i];
                count++;
            }
        }
        if (count == 0) return Array.Empty<HotelSearchItemResponse>();
        for (int i = 0; i < dim; i++) userVec[i] /= count;


        var norm = Math.Sqrt(userVec.Select(x => x * x).Sum());
        if (norm > 0)
        {
            for (int i = 0; i < dim; i++) userVec[i] = (float)(userVec[i] / norm);
        }

        static double Dot(float[] a, float[] b)
        {
            var len = Math.Min(a.Length, b.Length);
            double sum = 0;
            for (int i = 0; i < len; i++) sum += a[i] * b[i];
            return sum;
        }

        var ranked = hotels
            .Where(h => !interactedIds.Contains(h.Id))
            .Select(h =>
            {
                hotelVectorMap.TryGetValue(h.Id, out var vec);
                var score = vec is null ? double.MinValue : Dot(userVec, vec);
                return (hotel: h, score);
            })
            .Where(x => x.score > double.MinValue)
            .OrderByDescending(x => x.score)
            .Take(top)
            .Select(x => x.hotel)
            .ToList();


        if (ranked.Count == 0)
        {
            ranked = hotels
                .OrderByDescending(h => h.Rating)
                .ThenBy(h => h.RoomTypes.OrderBy(rt => rt.BasePrice).FirstOrDefault()?.BasePrice ?? decimal.MaxValue)
                .Take(top)
                .ToList();
        }

        return ranked.Select(ToResponse).ToList();
    }

    private static HotelSearchItemResponse ToResponse(Hotel h)
    {
        var minPrice = h.RoomTypes.OrderBy(rt => rt.BasePrice).FirstOrDefault()?.BasePrice ?? 0m;
        var thumb = h.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault() ?? string.Empty;
        var tags = h.HotelTags.Select(ht => new TagResponse { Id = ht.TagId, Name = ht.Tag.Name }).ToList();

        return new HotelSearchItemResponse
        {
            Id = h.Id,
            Name = h.Name,
            City = h.City.Name,
            FromPrice = minPrice,
            Rating = (double)h.Rating,
            ReviewCount = h.ReviewCount,
            ThumbnailUrl = thumb,
            HasAvailability = true,
            Tags = tags
        };
    }
}
