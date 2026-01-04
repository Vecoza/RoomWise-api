using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoomWise.Api.Data;
using RoomWise.Model;

namespace RoomWise.Api.SeedData;

public static class DemoSeedExtensions
{
    public static async Task SeedDemoDataAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<DataContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        async Task ResetIdentityAsync(string table, string pkColumn = "Id")
        {
            // Attempt to align the sequence with MAX(pk) without failing startup if sequence/column differs.
            var sql = $@"
DO $$
DECLARE seq text;
BEGIN
  SELECT pg_get_serial_sequence('""{table}""', '""{pkColumn}""') INTO seq;
  IF seq IS NOT NULL THEN
    PERFORM setval(seq, (SELECT COALESCE(MAX(""{pkColumn}""), 0) + 1 FROM ""{table}""), false);
  END IF;
END$$;";
            try
            {
                await ctx.Database.ExecuteSqlRawAsync(sql);
            }
            catch
            {
                // Ignore if the table/column does not have a sequence or names differ
            }
        }

        async Task EnsureEmailConfirmedAsync(AppUser user)
        {
            if (user.EmailConfirmed) return;
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        // 0) DEMO GUEST USER
        var demoEmail = "vecaTest@gmail.com";
        var demoUser = await userManager.FindByEmailAsync(demoEmail);
        if (demoUser is null)
        {
            demoUser = new AppUser
            {
                UserName = demoEmail,
                Email = demoEmail
            };

            var res = await userManager.CreateAsync(demoUser, "VecaTest123!");
            if (!res.Succeeded)
                throw new InvalidOperationException("Failed to create demo guest user: " +
                    string.Join(",", res.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(demoUser, AppRoles.Guest);
        }

        await EnsureEmailConfirmedAsync(demoUser);

        var demoUserId = demoUser.Id;

        // 1) HOTELS (requires Cities already seeded)
        if (!await ctx.Hotels.AnyAsync())
        {
            var sarajevoId = await ctx.Cities
                .Where(c => c.Name == "Sarajevo")
                .Select(c => c.Id)
                .FirstAsync();

            var mostarId = await ctx.Cities
                .Where(c => c.Name == "Mostar")
                .Select(c => c.Id)
                .FirstAsync();

            ctx.Hotels.AddRange(
                new Hotel
                {
                    Id = 1,
                    Name = "HanStay Sarajevo Center",
                    Description = "Modern business hotel in the center of Sarajevo with great Wi-Fi and breakfast.",
                    CityId = sarajevoId,
                    AddressLine = "Maršala Tita 15",
                    Rating = 4,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Id = 2,
                    Name = "HanStay Mostar Riverside",
                    Description = "Hotel by the Neretva river with old bridge views.",
                    CityId = mostarId,
                    AddressLine = "Onešćukova 7",
                    Rating = 4,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Id = 3,
                    Name = "Adriatic Breeze Split",
                    Description = "Coastal hotel with sea-view rooms and beach access.",
                    CityId = mostarId,
                    AddressLine = "Obala kneza 1",
                    Rating = 5,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Id = 4,
                    Name = "Mountain Chalet Jahorina",
                    Description = "Ski-in/ski-out lodge with spa and fireplace lounge.",
                    CityId = sarajevoId,
                    AddressLine = "Ski put 12",
                    Rating = 4,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Id = 5,
                    Name = "Old Town Sarajevo Suites",
                    Description = "Boutique suites near the historic bazaar with modern comforts.",
                    CityId = sarajevoId,
                    AddressLine = "Ferhadija 21",
                    Rating = 4,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Id = 6,
                    Name = "Riverstone Mostar Boutique",
                    Description = "Cozy riverside hotel with views of the old bridge.",
                    CityId = mostarId,
                    AddressLine = "Rade Bitange 4",
                    Rating = 4,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Id = 7,
                    Name = "Green Hills Sarajevo Resort",
                    Description = "Resort-style stay with wellness center and panoramic city views.",
                    CityId = sarajevoId,
                    AddressLine = "Trebevic 7",
                    Rating = 5,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Id = 8,
                    Name = "Neretva Garden Mostar",
                    Description = "Quiet retreat with garden courtyard and river walks nearby.",
                    CityId = mostarId,
                    AddressLine = "Mala Tepa 9",
                    Rating = 4,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Id = 9,
                    Name = "Sarajevo Skyline Hotel",
                    Description = "Modern high-rise hotel with business amenities and city views.",
                    CityId = sarajevoId,
                    AddressLine = "Zmaja od Bosne 12",
                    Rating = 4,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Id = 10,
                    Name = "Mostar Heritage Inn",
                    Description = "Traditional stone inn steps away from the old town.",
                    CityId = mostarId,
                    AddressLine = "Kujundziluk 3",
                    Rating = 5,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Id = 11,
                    Name = "Bascarsija Courtyard Hotel",
                    Description = "Charming courtyard hotel in the heart of the old city.",
                    CityId = sarajevoId,
                    AddressLine = "Kazandziluk 18",
                    Rating = 4,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Id = 12,
                    Name = "Stari Most Riverside",
                    Description = "Riverside stay with terrace dining and sunset views.",
                    CityId = mostarId,
                    AddressLine = "Ricanova 5",
                    Rating = 5,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Id = 13,
                    Name = "Sarajevo Business Hub",
                    Description = "Business-focused hotel with meeting rooms and fast Wi-Fi.",
                    CityId = sarajevoId,
                    AddressLine = "Hamdije Kresevljakovica 34",
                    Rating = 4,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Id = 14,
                    Name = "Mostar Panorama Lodge",
                    Description = "Hillside lodge with panoramic views of the valley.",
                    CityId = mostarId,
                    AddressLine = "Kneza Domagoja 2",
                    Rating = 4,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("Hotels");
        }

        // Hotel administrators (one per hotel)
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(AppRoles.Administrator))
        {
            await roleManager.CreateAsync(new IdentityRole(AppRoles.Administrator));
        }

        var hotels = await ctx.Hotels.AsNoTracking().ToListAsync();
        foreach (var hotel in hotels)
        {
            var adminEmail = $"admin{hotel.Id}@roomwise.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser is null)
            {
                adminUser = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail
                };
                var res = await userManager.CreateAsync(adminUser, "HotelAdmin123!");
                if (!res.Succeeded)
                    throw new InvalidOperationException("Failed to create hotel admin user: " +
                        string.Join(",", res.Errors.Select(e => e.Description)));
            }

            if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Administrator))
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Administrator);
            }

            await EnsureEmailConfirmedAsync(adminUser);

            var existing = await ctx.HotelAdmins.FirstOrDefaultAsync(ha => ha.HotelId == hotel.Id);
            if (existing is null)
            {
                ctx.HotelAdmins.Add(new HotelAdmin
                {
                    HotelId = hotel.Id,
                    UserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow
                });
                await ctx.SaveChangesAsync();
            }
        }

        // 2) ROOM TYPES
        if (!await ctx.RoomTypes.AnyAsync())
        {
            ctx.RoomTypes.AddRange(
                new RoomType
                {
                    Id = 1,
                    HotelId = 1,
                    Name = "Standard Double",
                    BedType = "Queen",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 80m,
                    Currency = "EUR",
                    Stock = 10,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 2,
                    HotelId = 1,
                    Name = "Deluxe Double",
                    BedType = "King",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 120m,
                    Currency = "EUR",
                    Stock = 5,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 3,
                    HotelId = 2,
                    Name = "Standard Twin",
                    BedType = "Twin",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 70m,
                    Currency = "EUR",
                    Stock = 8,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 4,
                    HotelId = 2,
                    Name = "Riverside Suite",
                    BedType = "King",
                    Capacity = 3,
                    IsSmokingAllowed = false,
                    BasePrice = 140m,
                    Currency = "EUR",
                    Stock = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 5,
                    HotelId = 3,
                    Name = "Sea View Double",
                    BedType = "Queen",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 150m,
                    Currency = "EUR",
                    Stock = 8,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 6,
                    HotelId = 3,
                    Name = "Family Apartment",
                    BedType = "King + Sofa",
                    Capacity = 4,
                    IsSmokingAllowed = false,
                    BasePrice = 220m,
                    Currency = "EUR",
                    Stock = 4,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 7,
                    HotelId = 4,
                    Name = "Ski Studio",
                    BedType = "Queen",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 110m,
                    Currency = "EUR",
                    Stock = 6,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 8,
                    HotelId = 4,
                    Name = "Chalet Loft",
                    BedType = "King",
                    Capacity = 3,
                    IsSmokingAllowed = false,
                    BasePrice = 180m,
                    Currency = "EUR",
                    Stock = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 9,
                    HotelId = 5,
                    Name = "Classic Double",
                    BedType = "Queen",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 100m,
                    Currency = "EUR",
                    Stock = 10,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 10,
                    HotelId = 5,
                    Name = "Family Suite",
                    BedType = "King + Sofa",
                    Capacity = 4,
                    IsSmokingAllowed = false,
                    BasePrice = 170m,
                    Currency = "EUR",
                    Stock = 5,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 11,
                    HotelId = 6,
                    Name = "Standard Twin",
                    BedType = "Twin",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 80m,
                    Currency = "EUR",
                    Stock = 8,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 12,
                    HotelId = 6,
                    Name = "River Suite",
                    BedType = "King",
                    Capacity = 3,
                    IsSmokingAllowed = false,
                    BasePrice = 150m,
                    Currency = "EUR",
                    Stock = 4,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 13,
                    HotelId = 7,
                    Name = "Garden Double",
                    BedType = "Queen",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 110m,
                    Currency = "EUR",
                    Stock = 10,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 14,
                    HotelId = 7,
                    Name = "Wellness Suite",
                    BedType = "King",
                    Capacity = 3,
                    IsSmokingAllowed = false,
                    BasePrice = 190m,
                    Currency = "EUR",
                    Stock = 4,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 15,
                    HotelId = 8,
                    Name = "Courtyard Double",
                    BedType = "Queen",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 90m,
                    Currency = "EUR",
                    Stock = 9,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 16,
                    HotelId = 8,
                    Name = "Panorama Suite",
                    BedType = "King",
                    Capacity = 3,
                    IsSmokingAllowed = false,
                    BasePrice = 160m,
                    Currency = "EUR",
                    Stock = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 17,
                    HotelId = 9,
                    Name = "City Double",
                    BedType = "Queen",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 110m,
                    Currency = "EUR",
                    Stock = 12,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 18,
                    HotelId = 9,
                    Name = "Executive Suite",
                    BedType = "King",
                    Capacity = 3,
                    IsSmokingAllowed = false,
                    BasePrice = 200m,
                    Currency = "EUR",
                    Stock = 4,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 19,
                    HotelId = 10,
                    Name = "Heritage Double",
                    BedType = "Queen",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 100m,
                    Currency = "EUR",
                    Stock = 8,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 20,
                    HotelId = 10,
                    Name = "Stone Suite",
                    BedType = "King",
                    Capacity = 3,
                    IsSmokingAllowed = false,
                    BasePrice = 180m,
                    Currency = "EUR",
                    Stock = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 21,
                    HotelId = 11,
                    Name = "Bazaar Double",
                    BedType = "Queen",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 90m,
                    Currency = "EUR",
                    Stock = 10,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 22,
                    HotelId = 11,
                    Name = "Courtyard Suite",
                    BedType = "King",
                    Capacity = 3,
                    IsSmokingAllowed = false,
                    BasePrice = 170m,
                    Currency = "EUR",
                    Stock = 4,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 23,
                    HotelId = 12,
                    Name = "Riverside Double",
                    BedType = "Queen",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 100m,
                    Currency = "EUR",
                    Stock = 9,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 24,
                    HotelId = 12,
                    Name = "Bridge View Suite",
                    BedType = "King",
                    Capacity = 3,
                    IsSmokingAllowed = false,
                    BasePrice = 190m,
                    Currency = "EUR",
                    Stock = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 25,
                    HotelId = 13,
                    Name = "Business Double",
                    BedType = "Queen",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 120m,
                    Currency = "EUR",
                    Stock = 12,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 26,
                    HotelId = 13,
                    Name = "Conference Suite",
                    BedType = "King",
                    Capacity = 3,
                    IsSmokingAllowed = false,
                    BasePrice = 210m,
                    Currency = "EUR",
                    Stock = 4,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 27,
                    HotelId = 14,
                    Name = "Lodge Double",
                    BedType = "Queen",
                    Capacity = 2,
                    IsSmokingAllowed = false,
                    BasePrice = 100m,
                    Currency = "EUR",
                    Stock = 8,
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Id = 28,
                    HotelId = 14,
                    Name = "Panorama Family",
                    BedType = "King + Sofa",
                    Capacity = 4,
                    IsSmokingAllowed = false,
                    BasePrice = 180m,
                    Currency = "EUR",
                    Stock = 4,
                    CreatedAt = DateTime.UtcNow
                }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("RoomTypes");
        }

        // 2a) ROOM TYPE IMAGES
        if (!await ctx.RoomTypeImages.AnyAsync())
        {
            ctx.RoomTypeImages.AddRange(
                        // Hotel 1 room types
                        new RoomTypeImage { Id = 1, RoomTypeId = 1, Url = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ0JxgI2qCHTsxA7QPfdfjYhu9rf6CT_-1mAA&s", SortOrder = 1 },
                        new RoomTypeImage { Id = 2, RoomTypeId = 1, Url = "https://media.istockphoto.com/id/174767532/photo/hotel-room.jpg?s=612x612&w=0&k=20&c=2BCNeFcX5PGzCxfZKXewhI_y2C9R7Jw_tzVYCXmRRCE=", SortOrder = 2 },

                        new RoomTypeImage { Id = 3, RoomTypeId = 2, Url = "https://t3.ftcdn.net/jpg/02/71/08/28/360_F_271082810_CtbTjpnOU3vx43ngAKqpCPUBx25udBrg.jpg", SortOrder = 1 },
                        new RoomTypeImage { Id = 4, RoomTypeId = 2, Url = "https://media.istockphoto.com/id/627892060/photo/hotel-room-suite-with-view.jpg?s=612x612&w=0&k=20&c=YBwxnGH3MkOLLpBKCvWAD8F__T-ypznRUJ_N13Zb1cU=", SortOrder = 2 },

                        // Hotel 2 room types
                        new RoomTypeImage { Id = 5, RoomTypeId = 3, Url = "https://img.freepik.com/free-photo/small-hotel-room-interior-with-double-bed-bathroom_1262-12489.jpg?semt=ais_hybrid&w=740&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 6, RoomTypeId = 3, Url = "https://static01.nyt.com/images/2019/03/24/travel/24trending-shophotels1/24trending-shophotels1-superJumbo.jpg", SortOrder = 2 },

                        new RoomTypeImage { Id = 7, RoomTypeId = 4, Url = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSnHFyGj0c1K-Mk106ZGT-juvcp-4Z8aMocHw&s", SortOrder = 1 },
                        new RoomTypeImage { Id = 8, RoomTypeId = 4, Url = "https://images.rawpixel.com/image_800/cHJpdmF0ZS9sci9pbWFnZXMvd2Vic2l0ZS8yMDIyLTA1L3AtMzEyLXRlZDY2OTYtY2hpbS5qcGc.jpg", SortOrder = 2 },

                        // Hotel 3 room types
                        new RoomTypeImage { Id = 9, RoomTypeId = 5, Url = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRgpTMKEwRIQFjkvBCLeSqtPXiFrQL5YYuCTQ&s", SortOrder = 1 },
                        new RoomTypeImage { Id = 10, RoomTypeId = 5, Url = "https://cdn.prod.website-files.com/62b1b17308b0d74291186304/672f4fbfb3638bf1cc97ce0f_672f498462cbddaad472142e_double%2520room%2520hote%2527.png", SortOrder = 2 },

                        new RoomTypeImage { Id = 11, RoomTypeId = 6, Url = "https://media.cnn.com/api/v1/images/stellar/prod/4b-mandarin-oriental-jumeira-dubai-the-royal-penthouse-bedroom.jpg?q=w_1110,c_fill", SortOrder = 1 },
                        new RoomTypeImage { Id = 12, RoomTypeId = 6, Url = "https://watergatebay.co.uk/storage/media/2024/06/12/HgiQGx8sWe/lg-sea-view-suite-twin-baths.jpg", SortOrder = 2 },

                        // Hotel 4 room types
                        new RoomTypeImage { Id = 13, RoomTypeId = 7, Url = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTggKCR1qXn8NtdAuB-yqg1wmKAAN-WLJvBJw&s", SortOrder = 1 },
                        new RoomTypeImage { Id = 14, RoomTypeId = 7, Url = "https://symphony.cdn.tambourine.com/menger-hotel-redesign/media/menger-hotel-gallery-10-5d37218c27264.jpg", SortOrder = 2 },

                        new RoomTypeImage { Id = 15, RoomTypeId = 8, Url = "https://www.millenniumhotels.com/mhb-media/new-destinations/eu-and-uk/united-kingdom/millennium-hotel-london-knightsbridge/rooms/kni---1280-x-568-signature.auto?rev=a31a67ec245542b2b93e9595b35f5df6", SortOrder = 1 },
                        new RoomTypeImage { Id = 16, RoomTypeId = 8, Url = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSFG2pVGkRMV-Cqb4HAS1dS9RsFF3Ii-Tf2Xg&s", SortOrder = 2 },

                        // Hotel 5 room types
                        new RoomTypeImage { Id = 17, RoomTypeId = 9, Url = "https://images.unsplash.com/photo-1505691938895-1758d7feb511?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 18, RoomTypeId = 9, Url = "https://images.unsplash.com/photo-1507089947368-19c1da9775ae?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },
                        new RoomTypeImage { Id = 19, RoomTypeId = 10, Url = "https://images.unsplash.com/photo-1501117716987-c8e1ecb2101e?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 20, RoomTypeId = 10, Url = "https://images.unsplash.com/photo-1493809842364-78817add7ffb?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },

                        // Hotel 6 room types
                        new RoomTypeImage { Id = 21, RoomTypeId = 11, Url = "https://images.unsplash.com/photo-1505691723518-36a5ac3be353?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 22, RoomTypeId = 11, Url = "https://images.unsplash.com/photo-1519710164239-da123dc03ef4?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },
                        new RoomTypeImage { Id = 23, RoomTypeId = 12, Url = "https://images.unsplash.com/photo-1444201983204-c43cbd584d93?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 24, RoomTypeId = 12, Url = "https://images.unsplash.com/photo-1507679799987-c73779587ccf?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },

                        // Hotel 7 room types
                        new RoomTypeImage { Id = 25, RoomTypeId = 13, Url = "https://images.unsplash.com/photo-1501876725168-00c445821c9e?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 26, RoomTypeId = 13, Url = "https://images.unsplash.com/photo-1505691938895-1758d7feb511?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },
                        new RoomTypeImage { Id = 27, RoomTypeId = 14, Url = "https://images.unsplash.com/photo-1507089947368-19c1da9775ae?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 28, RoomTypeId = 14, Url = "https://images.unsplash.com/photo-1501117716987-c8e1ecb2101e?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },

                        // Hotel 8 room types
                        new RoomTypeImage { Id = 29, RoomTypeId = 15, Url = "https://images.unsplash.com/photo-1493809842364-78817add7ffb?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 30, RoomTypeId = 15, Url = "https://images.unsplash.com/photo-1505691723518-36a5ac3be353?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },
                        new RoomTypeImage { Id = 31, RoomTypeId = 16, Url = "https://images.unsplash.com/photo-1519710164239-da123dc03ef4?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 32, RoomTypeId = 16, Url = "https://images.unsplash.com/photo-1444201983204-c43cbd584d93?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },

                        // Hotel 9 room types
                        new RoomTypeImage { Id = 33, RoomTypeId = 17, Url = "https://images.unsplash.com/photo-1507679799987-c73779587ccf?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 34, RoomTypeId = 17, Url = "https://images.unsplash.com/photo-1501876725168-00c445821c9e?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },
                        new RoomTypeImage { Id = 35, RoomTypeId = 18, Url = "https://images.unsplash.com/photo-1505691938895-1758d7feb511?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 36, RoomTypeId = 18, Url = "https://images.unsplash.com/photo-1507089947368-19c1da9775ae?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },

                        // Hotel 10 room types
                        new RoomTypeImage { Id = 37, RoomTypeId = 19, Url = "https://images.unsplash.com/photo-1501117716987-c8e1ecb2101e?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 38, RoomTypeId = 19, Url = "https://images.unsplash.com/photo-1493809842364-78817add7ffb?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },
                        new RoomTypeImage { Id = 39, RoomTypeId = 20, Url = "https://images.unsplash.com/photo-1505691723518-36a5ac3be353?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 40, RoomTypeId = 20, Url = "https://images.unsplash.com/photo-1519710164239-da123dc03ef4?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },

                        // Hotel 11 room types
                        new RoomTypeImage { Id = 41, RoomTypeId = 21, Url = "https://images.unsplash.com/photo-1444201983204-c43cbd584d93?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 42, RoomTypeId = 21, Url = "https://images.unsplash.com/photo-1507679799987-c73779587ccf?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },
                        new RoomTypeImage { Id = 43, RoomTypeId = 22, Url = "https://images.unsplash.com/photo-1501876725168-00c445821c9e?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 44, RoomTypeId = 22, Url = "https://images.unsplash.com/photo-1505691938895-1758d7feb511?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },

                        // Hotel 12 room types
                        new RoomTypeImage { Id = 45, RoomTypeId = 23, Url = "https://images.unsplash.com/photo-1507089947368-19c1da9775ae?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 46, RoomTypeId = 23, Url = "https://images.unsplash.com/photo-1501117716987-c8e1ecb2101e?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },
                        new RoomTypeImage { Id = 47, RoomTypeId = 24, Url = "https://images.unsplash.com/photo-1493809842364-78817add7ffb?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 48, RoomTypeId = 24, Url = "https://images.unsplash.com/photo-1505691723518-36a5ac3be353?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },

                        // Hotel 13 room types
                        new RoomTypeImage { Id = 49, RoomTypeId = 25, Url = "https://images.unsplash.com/photo-1519710164239-da123dc03ef4?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 50, RoomTypeId = 25, Url = "https://images.unsplash.com/photo-1444201983204-c43cbd584d93?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },
                        new RoomTypeImage { Id = 51, RoomTypeId = 26, Url = "https://images.unsplash.com/photo-1507679799987-c73779587ccf?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 52, RoomTypeId = 26, Url = "https://images.unsplash.com/photo-1501876725168-00c445821c9e?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },

                        // Hotel 14 room types
                        new RoomTypeImage { Id = 53, RoomTypeId = 27, Url = "https://images.unsplash.com/photo-1505691938895-1758d7feb511?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 54, RoomTypeId = 27, Url = "https://images.unsplash.com/photo-1507089947368-19c1da9775ae?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 },
                        new RoomTypeImage { Id = 55, RoomTypeId = 28, Url = "https://images.unsplash.com/photo-1501117716987-c8e1ecb2101e?auto=format&fit=crop&w=1200&q=80", SortOrder = 1 },
                        new RoomTypeImage { Id = 56, RoomTypeId = 28, Url = "https://images.unsplash.com/photo-1493809842364-78817add7ffb?auto=format&fit=crop&w=1200&q=80", SortOrder = 2 }
                    );

            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("RoomTypeImages");
        }

        // 3) ROOM RATES
        if (!await ctx.RoomRates.AnyAsync())
        {
            ctx.RoomRates.AddRange(
                new RoomRate
                {
                    Id = 1,
                    RoomTypeId = 1,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 11, 30),
                    Price = 90m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 2,
                    RoomTypeId = 2,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 11, 30),
                    Price = 130m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 3,
                    RoomTypeId = 3,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 11, 30),
                    Price = 80m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 4,
                    RoomTypeId = 4,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 150m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 5,
                    RoomTypeId = 5,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 160m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 6,
                    RoomTypeId = 6,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 240m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 7,
                    RoomTypeId = 7,
                    StartDate = new DateTime(2025, 12, 1),
                    EndDate = new DateTime(2026, 03, 31),
                    Price = 130m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 8,
                    RoomTypeId = 8,
                    StartDate = new DateTime(2025, 12, 1),
                    EndDate = new DateTime(2026, 03, 31),
                    Price = 210m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 9,
                    RoomTypeId = 9,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 100m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 10,
                    RoomTypeId = 10,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 180m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 11,
                    RoomTypeId = 11,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 90m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 12,
                    RoomTypeId = 12,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 160m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 13,
                    RoomTypeId = 13,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 120m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 14,
                    RoomTypeId = 14,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 200m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 15,
                    RoomTypeId = 15,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 90m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 16,
                    RoomTypeId = 16,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 170m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 17,
                    RoomTypeId = 17,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 110m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 18,
                    RoomTypeId = 18,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 210m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 19,
                    RoomTypeId = 19,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 100m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 20,
                    RoomTypeId = 20,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 180m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 21,
                    RoomTypeId = 21,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 100m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 22,
                    RoomTypeId = 22,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 170m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 23,
                    RoomTypeId = 23,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 110m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 24,
                    RoomTypeId = 24,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 190m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 25,
                    RoomTypeId = 25,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 130m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 26,
                    RoomTypeId = 26,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 220m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 27,
                    RoomTypeId = 27,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 110m,
                    Currency = "EUR"
                },
                new RoomRate
                {
                    Id = 28,
                    RoomTypeId = 28,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    Price = 190m,
                    Currency = "EUR"
                }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("RoomRates");
        }

        // 4) ADD-ONS
        if (!await ctx.AddOns.AnyAsync())
        {
            ctx.AddOns.AddRange(
                new AddOn
                {
                    Id = 1,
                    HotelId = 1,
                    Name = "Breakfast buffet",
                    Description = "Buffet breakfast in the hotel restaurant.",
                    Price = 10m,
                    PricingModel = "PerNight",
                    IsActive = true
                },
                new AddOn
                {
                    Id = 2,
                    HotelId = 1,
                    Name = "Parking",
                    Description = "Underground garage parking.",
                    Price = 8m,
                    PricingModel = "PerNight",
                    IsActive = true
                },
                new AddOn
                {
                    Id = 3,
                    HotelId = 2,
                    Name = "Airport shuttle",
                    Description = "One-way shuttle from/to airport.",
                    Price = 25m,
                    PricingModel = "PerStay",
                    IsActive = true
                },
                new AddOn
                {
                    Id = 4,
                    HotelId = 3,
                    Name = "Beach club access",
                    Description = "Day pass to the partnered beach club.",
                    Price = 15m,
                    PricingModel = "PerDay",
                    IsActive = true
                },
                new AddOn
                {
                    Id = 5,
                    HotelId = 4,
                    Name = "Ski pass",
                    Description = "Daily ski lift pass.",
                    Price = 30m,
                    PricingModel = "PerNight",
                    IsActive = true
                },
                new AddOn
                {
                    Id = 6,
                    HotelId = 4,
                    Name = "Spa access",
                    Description = "Access to sauna and hot tub.",
                    Price = 12m,
                    PricingModel = "PerStay",
                    IsActive = true
                }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("AddOns");
        }

        // 5) FACILITIES + HOTEL FACILITIES
        if (!await ctx.Facilities.AnyAsync())
        {
            var wifi = new Facility { Id = 1, Code = "wifi", Name = "Free Wi-Fi" };
            var pool = new Facility { Id = 2, Code = "pool", Name = "Pool" };
            var spa = new Facility { Id = 3, Code = "spa", Name = "Spa" };
            var gym = new Facility { Id = 4, Code = "gym", Name = "Gym" };
            var parking = new Facility { Id = 5, Code = "parking", Name = "Parking" };
            ctx.Facilities.AddRange(wifi, pool, spa, gym, parking);
            await ctx.SaveChangesAsync();

            ctx.HotelFacilities.AddRange(
                new HotelFacility { HotelId = 1, FacilityId = wifi.Id },
                new HotelFacility { HotelId = 1, FacilityId = gym.Id },
                new HotelFacility { HotelId = 1, FacilityId = parking.Id },
                new HotelFacility { HotelId = 2, FacilityId = wifi.Id },
                new HotelFacility { HotelId = 2, FacilityId = pool.Id },
                new HotelFacility { HotelId = 3, FacilityId = wifi.Id },
                new HotelFacility { HotelId = 3, FacilityId = pool.Id },
                new HotelFacility { HotelId = 3, FacilityId = spa.Id },
                new HotelFacility { HotelId = 4, FacilityId = wifi.Id },
                new HotelFacility { HotelId = 4, FacilityId = spa.Id },
                new HotelFacility { HotelId = 4, FacilityId = parking.Id }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("Facilities");
            await ResetIdentityAsync("HotelFacilities", "HotelId");
        }

        // 6) ROOM AVAILABILITY (Nov 2025–Jan 2026)
        if (!await ctx.RoomAvailabilities.AnyAsync())
        {
            var start = new DateTime(2025, 11, 1);
            var end = new DateTime(2026, 1, 31);

            var roomTypesSeed = await ctx.RoomTypes.AsNoTracking().Select(rt => new { rt.Id, rt.Stock }).ToListAsync();
            var rows = new List<RoomAvailability>();

            foreach (var rt in roomTypesSeed)
            {
                for (var d = start; d <= end; d = d.AddDays(1))
                {
                    rows.Add(new RoomAvailability
                    {
                        RoomTypeId = rt.Id,
                        Date = d,
                        Available = Math.Max(1, rt.Stock)
                    });
                }
            }

            await ctx.RoomAvailabilities.AddRangeAsync(rows);
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("RoomAvailabilities");
        }

        // 6b) Ensure January 2026 availability exists for all room types
        {
            var janStart = new DateTime(2026, 1, 1);
            var janEnd = new DateTime(2026, 1, 31);

            var roomTypesSeed = await ctx.RoomTypes.AsNoTracking()
                .Select(rt => new { rt.Id, rt.Stock })
                .ToListAsync();

            var existing = await ctx.RoomAvailabilities
                .Where(a => a.Date >= janStart && a.Date <= janEnd)
                .Select(a => new { a.RoomTypeId, a.Date })
                .ToListAsync();

            var existingSet = new HashSet<(int RoomTypeId, DateTime Date)>(
                existing.Select(x => (x.RoomTypeId, x.Date)));

            var missing = new List<RoomAvailability>();
            foreach (var rt in roomTypesSeed)
            {
                for (var d = janStart; d <= janEnd; d = d.AddDays(1))
                {
                    if (existingSet.Contains((rt.Id, d))) continue;

                    missing.Add(new RoomAvailability
                    {
                        RoomTypeId = rt.Id,
                        Date = d,
                        Available = Math.Max(1, rt.Stock)
                    });
                }
            }

            if (missing.Count > 0)
            {
                await ctx.RoomAvailabilities.AddRangeAsync(missing);
                await ctx.SaveChangesAsync();
                await ResetIdentityAsync("RoomAvailabilities");
            }
        }

        // 7) PROMOTIONS
        if (!await ctx.Promotions.AnyAsync())
        {
            ctx.Promotions.AddRange(
                new Promotion
                {
                    Id = 1,
                    HotelId = 1,
                    Title = "Autumn Special",
                    Description = "10% off for stays in November.",
                    DiscountPercent = 10,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 11, 30),
                    IsActive = true
                },
                new Promotion
                {
                    Id = 2,
                    HotelId = 3,
                    Title = "Sea Escape",
                    Description = "10% off midweek stays.",
                    DiscountPercent = 10,
                    StartDate = new DateTime(2025, 11, 5),
                    EndDate = new DateTime(2025, 12, 15),
                    IsActive = true
                },
                new Promotion
                {
                    Id = 3,
                    HotelId = 4,
                    Title = "Early Ski Saver",
                    Description = "20% off December bookings.",
                    DiscountPercent = 20,
                    StartDate = new DateTime(2025, 12, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    IsActive = true
                },
                new Promotion
                {
                    Id = 4,
                    HotelId = 5,
                    Title = "Old Town Weekend",
                    Description = "10% off weekend stays.",
                    DiscountPercent = 10,
                    StartDate = new DateTime(2025, 10, 1),
                    EndDate = new DateTime(2025, 12, 15),
                    IsActive = true
                },
                new Promotion
                {
                    Id = 5,
                    HotelId = 6,
                    Title = "River View Deal",
                    Description = "10% off for riverside rooms.",
                    DiscountPercent = 10,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    IsActive = true
                },
                new Promotion
                {
                    Id = 6,
                    HotelId = 7,
                    Title = "Wellness Escape",
                    Description = "20% off spa and wellness packages.",
                    DiscountPercent = 20,
                    StartDate = new DateTime(2025, 11, 15),
                    EndDate = new DateTime(2026, 1, 31),
                    IsActive = true
                },
                new Promotion
                {
                    Id = 7,
                    HotelId = 8,
                    Title = "Garden Calm",
                    Description = "10% off midweek stays.",
                    DiscountPercent = 10,
                    StartDate = new DateTime(2025, 10, 15),
                    EndDate = new DateTime(2025, 12, 31),
                    IsActive = true
                },
                new Promotion
                {
                    Id = 8,
                    HotelId = 9,
                    Title = "Skyline Business",
                    Description = "10% off business stays in November.",
                    DiscountPercent = 10,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 11, 30),
                    IsActive = true
                },
                new Promotion
                {
                    Id = 9,
                    HotelId = 10,
                    Title = "Heritage Nights",
                    Description = "10% off for heritage stays.",
                    DiscountPercent = 10,
                    StartDate = new DateTime(2025, 11, 10),
                    EndDate = new DateTime(2025, 12, 20),
                    IsActive = true
                },
                new Promotion
                {
                    Id = 10,
                    HotelId = 11,
                    Title = "Courtyard Stay",
                    Description = "10% off for longer stays.",
                    DiscountPercent = 10,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 15),
                    IsActive = true
                },
                new Promotion
                {
                    Id = 11,
                    HotelId = 12,
                    Title = "Riverside Sunset",
                    Description = "10% off river view rooms.",
                    DiscountPercent = 10,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    IsActive = true
                },
                new Promotion
                {
                    Id = 12,
                    HotelId = 13,
                    Title = "Business Saver",
                    Description = "10% off corporate bookings.",
                    DiscountPercent = 10,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 11, 30),
                    IsActive = true
                },
                new Promotion
                {
                    Id = 13,
                    HotelId = 14,
                    Title = "Panorama Escape",
                    Description = "10% off winter stays.",
                    DiscountPercent = 10,
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    IsActive = true
                }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("Promotions");
        }

        // 7) TAGS + HOTEL TAGS
        if (!await ctx.Tags.AnyAsync())
        {
            var tagBusiness = new Tag { Id = 1, Name = "Business" };
            var tagFamily = new Tag { Id = 2, Name = "Family" };
            var tagSpa = new Tag { Id = 3, Name = "Spa" };
            var tagBeach = new Tag { Id = 4, Name = "Beach" };
            var tagSki = new Tag { Id = 5, Name = "Ski" };

            ctx.Tags.AddRange(tagBusiness, tagFamily, tagSpa, tagBeach, tagSki);
            await ctx.SaveChangesAsync();

            ctx.HotelTags.AddRange(
                new HotelTag { HotelId = 1, TagId = 1 },
                new HotelTag { HotelId = 1, TagId = 2 },
                new HotelTag { HotelId = 2, TagId = 2 },
                new HotelTag { HotelId = 3, TagId = 4 },
                new HotelTag { HotelId = 3, TagId = 2 },
                new HotelTag { HotelId = 4, TagId = 5 },
                new HotelTag { HotelId = 4, TagId = 3 },
                new HotelTag { HotelId = 5, TagId = 1 },
                new HotelTag { HotelId = 5, TagId = 2 },
                new HotelTag { HotelId = 6, TagId = 2 },
                new HotelTag { HotelId = 6, TagId = 4 },
                new HotelTag { HotelId = 7, TagId = 3 },
                new HotelTag { HotelId = 7, TagId = 2 },
                new HotelTag { HotelId = 8, TagId = 2 },
                new HotelTag { HotelId = 8, TagId = 4 },
                new HotelTag { HotelId = 9, TagId = 1 },
                new HotelTag { HotelId = 10, TagId = 2 },
                new HotelTag { HotelId = 10, TagId = 4 },
                new HotelTag { HotelId = 11, TagId = 1 },
                new HotelTag { HotelId = 11, TagId = 2 },
                new HotelTag { HotelId = 12, TagId = 4 },
                new HotelTag { HotelId = 13, TagId = 1 },
                new HotelTag { HotelId = 14, TagId = 2 },
                new HotelTag { HotelId = 14, TagId = 3 }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("Tags");
        }

        // 8) HOTEL IMAGES
        if (!await ctx.HotelImages.AnyAsync())
        {
            ctx.HotelImages.AddRange(
                new HotelImage
                {
                    Id = 1,
                    HotelId = 1,
                    Url = "https://www.dangleterre.com/uploads/media/1200x630/00/370-_DSF2441_SAM_WS2_aRGB_High-1600px.jpg?v=1-0",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 2,
                    HotelId = 1,
                    Url = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRB6qCEHozSkz53GtEubo8U17Ao2rmmHzClGw&s",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 3,
                    HotelId = 1,
                    Url = "https://cf.bstatic.com/xdata/images/hotel/max1024x768/241307603.jpg?k=93aba2b8c4909e90cb6e5e55ff1676dd6a944139423ba99e0ad32cb589606f28&o=",
                    SortOrder = 3
                },
                new HotelImage
                {
                    Id = 4,
                    HotelId = 2,
                    Url = "https://m.ahstatic.com/is/image/accorhotels/HCM_P_8147067:4by3?fmt=jpg&op_usm=1.75,0.3,2,0&resMode=sharp2&iccEmbed=true&icc=sRGB&dpr=on,1.5&wid=335&hei=251&qlt=80",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 5,
                    HotelId = 2,
                    Url = "https://dynamic-media-cdn.tripadvisor.com/media/photo-o/21/89/11/15/hotel-facade.jpg?w=900&h=500&s=1",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 6,
                    HotelId = 2,
                    Url = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSDrupvySWTfYQhppPRES2xRt-GZQhZbZf1jw&s",
                    SortOrder = 3
                },
                new HotelImage
                {
                    Id = 7,
                    HotelId = 3,
                    Url = "https://www.luxuryabode.com/mona/img/hotels.jpg",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 8,
                    HotelId = 3,
                    Url = "https://www.luxuryabode.com/mona/img/hotels.jpg",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 9,
                    HotelId = 3,
                    Url = "https://cache.marriott.com/content/dam/marriott-digital/rz/emea/hws/b/bcnrz/en_us/photo/unlimited/assets/50514355-arts-hotel-april-2018-02.png",
                    SortOrder = 3
                },
                new HotelImage
                {
                    Id = 10,
                    HotelId = 4,
                    Url = "https://cache.marriott.com/content/dam/marriott-digital/rz/emea/hws/b/bcnrz/en_us/photo/unlimited/assets/50514355-arts-hotel-april-2018-02.png",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 11,
                    HotelId = 4,
                    Url = "https://files.selar.co/product-images/2024/products/tidaconsulting/hotel-financial-model-selar.co-6644d050ec39f.jpeg",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 12,
                    HotelId = 4,
                    Url = "https://files.selar.co/product-images/2024/products/tidaconsulting/hotel-financial-model-selar.co-6644d050ec39f.jpeg",
                    SortOrder = 3
                },
                new HotelImage
                {
                    Id = 13,
                    HotelId = 5,
                    Url = "https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 14,
                    HotelId = 5,
                    Url = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 15,
                    HotelId = 6,
                    Url = "https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 16,
                    HotelId = 6,
                    Url = "https://images.unsplash.com/photo-1484154218962-a197022b5858?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 17,
                    HotelId = 7,
                    Url = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 18,
                    HotelId = 7,
                    Url = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 19,
                    HotelId = 8,
                    Url = "https://images.unsplash.com/photo-1505691723518-36a5ac3be353?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 20,
                    HotelId = 8,
                    Url = "https://images.unsplash.com/photo-1507089947368-19c1da9775ae?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 21,
                    HotelId = 9,
                    Url = "https://images.unsplash.com/photo-1501876725168-00c445821c9e?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 22,
                    HotelId = 9,
                    Url = "https://images.unsplash.com/photo-1501117716987-c8e1ecb2101e?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 23,
                    HotelId = 10,
                    Url = "https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 24,
                    HotelId = 10,
                    Url = "https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 25,
                    HotelId = 11,
                    Url = "https://images.unsplash.com/photo-1484154218962-a197022b5858?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 26,
                    HotelId = 11,
                    Url = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 27,
                    HotelId = 12,
                    Url = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 28,
                    HotelId = 12,
                    Url = "https://images.unsplash.com/photo-1505691723518-36a5ac3be353?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 29,
                    HotelId = 13,
                    Url = "https://images.unsplash.com/photo-1507089947368-19c1da9775ae?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 30,
                    HotelId = 13,
                    Url = "https://images.unsplash.com/photo-1501876725168-00c445821c9e?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 31,
                    HotelId = 14,
                    Url = "https://images.unsplash.com/photo-1501117716987-c8e1ecb2101e?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 32,
                    HotelId = 14,
                    Url = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?auto=format&fit=crop&w=1200&q=80",
                    SortOrder = 2
                }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("HotelImages");
        }

        // 9) PHONE CONTACTS
        if (!await ctx.PhoneContacts.AnyAsync())
        {
            ctx.PhoneContacts.AddRange(
                new PhoneContact
                {
                    Id = 1,
                    HotelId = 1,
                    Label = "Front desk",
                    PhoneNumber = "+387 33 123 456"
                },
                new PhoneContact
                {
                    Id = 2,
                    HotelId = 2,
                    Label = "Reception",
                    PhoneNumber = "+387 36 654 321"
                },
                new PhoneContact
                {
                    Id = 3,
                    HotelId = 3,
                    Label = "Front desk",
                    PhoneNumber = "+385 21 555 100"
                },
                new PhoneContact
                {
                    Id = 4,
                    HotelId = 4,
                    Label = "Reception",
                    PhoneNumber = "+387 33 555 200"
                },
                new PhoneContact
                {
                    Id = 5,
                    HotelId = 5,
                    Label = "Front desk",
                    PhoneNumber = "+387 33 555 300"
                },
                new PhoneContact
                {
                    Id = 6,
                    HotelId = 6,
                    Label = "Reception",
                    PhoneNumber = "+387 36 555 400"
                },
                new PhoneContact
                {
                    Id = 7,
                    HotelId = 7,
                    Label = "Front desk",
                    PhoneNumber = "+387 33 555 500"
                },
                new PhoneContact
                {
                    Id = 8,
                    HotelId = 8,
                    Label = "Reception",
                    PhoneNumber = "+387 36 555 600"
                },
                new PhoneContact
                {
                    Id = 9,
                    HotelId = 9,
                    Label = "Front desk",
                    PhoneNumber = "+387 33 555 700"
                },
                new PhoneContact
                {
                    Id = 10,
                    HotelId = 10,
                    Label = "Reception",
                    PhoneNumber = "+387 36 555 800"
                },
                new PhoneContact
                {
                    Id = 11,
                    HotelId = 11,
                    Label = "Front desk",
                    PhoneNumber = "+387 33 555 900"
                },
                new PhoneContact
                {
                    Id = 12,
                    HotelId = 12,
                    Label = "Reception",
                    PhoneNumber = "+387 36 555 010"
                },
                new PhoneContact
                {
                    Id = 13,
                    HotelId = 13,
                    Label = "Front desk",
                    PhoneNumber = "+387 33 555 020"
                },
                new PhoneContact
                {
                    Id = 14,
                    HotelId = 14,
                    Label = "Reception",
                    PhoneNumber = "+387 36 555 030"
                }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("PhoneContacts");
        }

        // 10) WISHLIST
        if (!await ctx.Wishlists.AnyAsync())
        {
            ctx.Wishlists.Add(
                new Wishlist
                {
                    Id = 1,
                    UserId = demoUserId,
                    HotelId = 1,
                    CreatedAt = DateTime.UtcNow
                }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("Wishlists");
        }

        // 11) DEMO RESERVATIONS (for statistics etc.)
        if (!await ctx.Reservations.AnyAsync())
        {
            var reservations = new List<Reservation>
            {
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 1,
                    RoomTypeId = 1,
                    CheckIn = new DateTime(2025, 11, 10),
                    CheckOut = new DateTime(2025, 11, 12),
                    Guests = 2,
                    Subtotal = 90m * 2, // 2 nights
                    Currency = "EUR",
                    Status = "Confirmed",
                    ConfirmationNumber = "RW-DEMO-0001",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 2,
                    RoomTypeId = 4,
                    CheckIn = new DateTime(2025, 11, 15),
                    CheckOut = new DateTime(2025, 11, 18),
                    Guests = 3,
                    Subtotal = 150m * 3,
                    Currency = "EUR",
                    Status = "Pending",
                    ConfirmationNumber = "RW-DEMO-0002",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 3,
                    RoomTypeId = 5,
                    CheckIn = new DateTime(2025, 12, 5),
                    CheckOut = new DateTime(2025, 12, 8),
                    Guests = 2,
                    Subtotal = 160m * 3,
                    Currency = "EUR",
                    Status = "Pending",
                    ConfirmationNumber = "RW-DEMO-0003",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 4,
                    RoomTypeId = 7,
                    CheckIn = new DateTime(2025, 12, 20),
                    CheckOut = new DateTime(2025, 12, 23),
                    Guests = 2,
                    Subtotal = 130m * 3,
                    Currency = "EUR",
                    Status = "Pending",
                    ConfirmationNumber = "RW-DEMO-0004",
                    CreatedAt = DateTime.UtcNow
                },
                // Past stay (completed)
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 2,
                    RoomTypeId = 3,
                    CheckIn = new DateTime(2024, 10, 5),
                    CheckOut = new DateTime(2024, 10, 8),
                    Guests = 2,
                    Subtotal = 120m * 3,
                    TaxesAndFees = 0m,
                    ServiceFee = 0m,
                    Currency = "EUR",
                    Status = "Completed",
                    ConfirmationNumber = "RW-DEMO-0005",
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                },
                // Another past stay (completed)
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 3,
                    RoomTypeId = 6,
                    CheckIn = new DateTime(2024, 9, 12),
                    CheckOut = new DateTime(2024, 9, 15),
                    Guests = 1,
                    Subtotal = 100m * 3,
                    TaxesAndFees = 0m,
                    ServiceFee = 0m,
                    Currency = "EUR",
                    Status = "Completed",
                    ConfirmationNumber = "RW-DEMO-0006",
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                },
                // Cancelled reservation
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 1,
                    RoomTypeId = 2,
                    CheckIn = new DateTime(2025, 1, 10),
                    CheckOut = new DateTime(2025, 1, 12),
                    Guests = 2,
                    Subtotal = 90m * 2,
                    TaxesAndFees = 0m,
                    ServiceFee = 0m,
                    Currency = "EUR",
                    Status = "Cancelled",
                    CancelledAt = DateTime.UtcNow.AddDays(-10),
                    ConfirmationNumber = "RW-DEMO-0007",
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },
                // Another cancelled reservation
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 4,
                    RoomTypeId = 8,
                    CheckIn = new DateTime(2025, 2, 5),
                    CheckOut = new DateTime(2025, 2, 7),
                    Guests = 1,
                    Subtotal = 110m * 2,
                    TaxesAndFees = 0m,
                    ServiceFee = 0m,
                    Currency = "EUR",
                    Status = "Cancelled",
                    CancelledAt = DateTime.UtcNow.AddDays(-5),
                    ConfirmationNumber = "RW-DEMO-0008",
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 5,
                    RoomTypeId = 9,
                    CheckIn = DateTime.UtcNow.AddDays(-20).Date,
                    CheckOut = DateTime.UtcNow.AddDays(-18).Date,
                    Guests = 2,
                    Subtotal = 100m * 2,
                    TaxesAndFees = 0m,
                    ServiceFee = 0m,
                    Currency = "EUR",
                    Status = "Completed",
                    ConfirmationNumber = "RW-DEMO-0009",
                    CreatedAt = DateTime.UtcNow.AddDays(-21)
                },
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 6,
                    RoomTypeId = 11,
                    CheckIn = DateTime.UtcNow.AddDays(-17).Date,
                    CheckOut = DateTime.UtcNow.AddDays(-15).Date,
                    Guests = 2,
                    Subtotal = 80m * 2,
                    TaxesAndFees = 0m,
                    ServiceFee = 0m,
                    Currency = "EUR",
                    Status = "Completed",
                    ConfirmationNumber = "RW-DEMO-0010",
                    CreatedAt = DateTime.UtcNow.AddDays(-18)
                },
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 7,
                    RoomTypeId = 13,
                    CheckIn = DateTime.UtcNow.AddDays(-16).Date,
                    CheckOut = DateTime.UtcNow.AddDays(-14).Date,
                    Guests = 2,
                    Subtotal = 110m * 2,
                    TaxesAndFees = 0m,
                    ServiceFee = 0m,
                    Currency = "EUR",
                    Status = "Completed",
                    ConfirmationNumber = "RW-DEMO-0011",
                    CreatedAt = DateTime.UtcNow.AddDays(-16)
                },
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 8,
                    RoomTypeId = 15,
                    CheckIn = DateTime.UtcNow.AddDays(-13).Date,
                    CheckOut = DateTime.UtcNow.AddDays(-11).Date,
                    Guests = 2,
                    Subtotal = 90m * 2,
                    TaxesAndFees = 0m,
                    ServiceFee = 0m,
                    Currency = "EUR",
                    Status = "Completed",
                    ConfirmationNumber = "RW-DEMO-0012",
                    CreatedAt = DateTime.UtcNow.AddDays(-13)
                },
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 9,
                    RoomTypeId = 17,
                    CheckIn = DateTime.UtcNow.AddDays(-12).Date,
                    CheckOut = DateTime.UtcNow.AddDays(-10).Date,
                    Guests = 2,
                    Subtotal = 110m * 2,
                    TaxesAndFees = 0m,
                    ServiceFee = 0m,
                    Currency = "EUR",
                    Status = "Completed",
                    ConfirmationNumber = "RW-DEMO-0013",
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                },
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 10,
                    RoomTypeId = 19,
                    CheckIn = DateTime.UtcNow.AddDays(-10).Date,
                    CheckOut = DateTime.UtcNow.AddDays(-8).Date,
                    Guests = 2,
                    Subtotal = 100m * 2,
                    TaxesAndFees = 0m,
                    ServiceFee = 0m,
                    Currency = "EUR",
                    Status = "Completed",
                    ConfirmationNumber = "RW-DEMO-0014",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 11,
                    RoomTypeId = 21,
                    CheckIn = DateTime.UtcNow.AddDays(-9).Date,
                    CheckOut = DateTime.UtcNow.AddDays(-7).Date,
                    Guests = 2,
                    Subtotal = 90m * 2,
                    TaxesAndFees = 0m,
                    ServiceFee = 0m,
                    Currency = "EUR",
                    Status = "Completed",
                    ConfirmationNumber = "RW-DEMO-0015",
                    CreatedAt = DateTime.UtcNow.AddDays(-9)
                },
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 12,
                    RoomTypeId = 23,
                    CheckIn = DateTime.UtcNow.AddDays(-8).Date,
                    CheckOut = DateTime.UtcNow.AddDays(-6).Date,
                    Guests = 2,
                    Subtotal = 100m * 2,
                    TaxesAndFees = 0m,
                    ServiceFee = 0m,
                    Currency = "EUR",
                    Status = "Completed",
                    ConfirmationNumber = "RW-DEMO-0016",
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 13,
                    RoomTypeId = 25,
                    CheckIn = DateTime.UtcNow.AddDays(-7).Date,
                    CheckOut = DateTime.UtcNow.AddDays(-5).Date,
                    Guests = 2,
                    Subtotal = 120m * 2,
                    TaxesAndFees = 0m,
                    ServiceFee = 0m,
                    Currency = "EUR",
                    Status = "Completed",
                    ConfirmationNumber = "RW-DEMO-0017",
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                },
                new Reservation
                {
                    PublicId = Guid.NewGuid(),
                    UserId = demoUserId,
                    HotelId = 14,
                    RoomTypeId = 27,
                    CheckIn = DateTime.UtcNow.AddDays(-6).Date,
                    CheckOut = DateTime.UtcNow.AddDays(-4).Date,
                    Guests = 2,
                    Subtotal = 100m * 2,
                    TaxesAndFees = 0m,
                    ServiceFee = 0m,
                    Currency = "EUR",
                    Status = "Completed",
                    ConfirmationNumber = "RW-DEMO-0018",
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                }
            };

            ctx.Reservations.AddRange(reservations);
            await ctx.SaveChangesAsync();

            // 11a) REVIEWS (after reservations so we have ReservationId FK)
            if (!await ctx.Reviews.AnyAsync())
            {
                var byHotel = reservations.GroupBy(r => r.HotelId)
                    .ToDictionary(g => g.Key, g => g.First().Id);

                ctx.Reviews.AddRange(
                    new Review
                    {
                        HotelId = 1,
                        ReservationId = byHotel[1],
                        UserId = demoUserId,
                        Rating = 5,
                        Title = "Great stay!",
                        Body = "Perfect location and friendly staff.",
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    },
                    new Review
                    {
                        HotelId = 2,
                        ReservationId = byHotel[2],
                        UserId = demoUserId,
                        Rating = 4,
                        Title = "Nice riverside spot",
                        Body = "Loved the view of the river, staff was helpful.",
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new Review
                    {
                        HotelId = 3,
                        ReservationId = byHotel[3],
                        UserId = demoUserId,
                        Rating = 5,
                        Title = "Beach vibes",
                        Body = "Perfect for a beach weekend; would come back.",
                        CreatedAt = DateTime.UtcNow.AddDays(-7)
                    },
                    new Review
                    {
                        HotelId = 4,
                        ReservationId = byHotel[4],
                        UserId = demoUserId,
                        Rating = 4,
                        Title = "Great for skiing",
                        Body = "Cozy lodge, easy access to the slopes.",
                        CreatedAt = DateTime.UtcNow.AddDays(-12)
                    },
                    new Review
                    {
                        HotelId = 5,
                        ReservationId = byHotel[5],
                        UserId = demoUserId,
                        Rating = 5,
                        Title = "Old town charm",
                        Body = "Lovely boutique stay near the bazaar.",
                        CreatedAt = DateTime.UtcNow.AddDays(-20)
                    },
                    new Review
                    {
                        HotelId = 6,
                        ReservationId = byHotel[6],
                        UserId = demoUserId,
                        Rating = 4,
                        Title = "Riverside comfort",
                        Body = "Peaceful rooms and great staff.",
                        CreatedAt = DateTime.UtcNow.AddDays(-17)
                    },
                    new Review
                    {
                        HotelId = 7,
                        ReservationId = byHotel[7],
                        UserId = demoUserId,
                        Rating = 5,
                        Title = "Wellness escape",
                        Body = "Spa was excellent, beautiful views.",
                        CreatedAt = DateTime.UtcNow.AddDays(-16)
                    },
                    new Review
                    {
                        HotelId = 8,
                        ReservationId = byHotel[8],
                        UserId = demoUserId,
                        Rating = 4,
                        Title = "Quiet and green",
                        Body = "Loved the garden and calm atmosphere.",
                        CreatedAt = DateTime.UtcNow.AddDays(-13)
                    },
                    new Review
                    {
                        HotelId = 9,
                        ReservationId = byHotel[9],
                        UserId = demoUserId,
                        Rating = 4,
                        Title = "Great city stay",
                        Body = "Modern rooms and convenient location.",
                        CreatedAt = DateTime.UtcNow.AddDays(-12)
                    },
                    new Review
                    {
                        HotelId = 10,
                        ReservationId = byHotel[10],
                        UserId = demoUserId,
                        Rating = 5,
                        Title = "Heritage gem",
                        Body = "Charming stone building and friendly hosts.",
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    },
                    new Review
                    {
                        HotelId = 11,
                        ReservationId = byHotel[11],
                        UserId = demoUserId,
                        Rating = 4,
                        Title = "Courtyard beauty",
                        Body = "Very cozy, perfect for a city break.",
                        CreatedAt = DateTime.UtcNow.AddDays(-9)
                    },
                    new Review
                    {
                        HotelId = 12,
                        ReservationId = byHotel[12],
                        UserId = demoUserId,
                        Rating = 5,
                        Title = "Sunset views",
                        Body = "Amazing terrace and river views.",
                        CreatedAt = DateTime.UtcNow.AddDays(-8)
                    },
                    new Review
                    {
                        HotelId = 13,
                        ReservationId = byHotel[13],
                        UserId = demoUserId,
                        Rating = 4,
                        Title = "Business ready",
                        Body = "Great Wi-Fi and meeting facilities.",
                        CreatedAt = DateTime.UtcNow.AddDays(-7)
                    },
                    new Review
                    {
                        HotelId = 14,
                        ReservationId = byHotel[14],
                        UserId = demoUserId,
                        Rating = 4,
                        Title = "Panorama views",
                        Body = "Beautiful hillside lodge with great views.",
                        CreatedAt = DateTime.UtcNow.AddDays(-6)
                    }
                );
                await ctx.SaveChangesAsync();
                await ResetIdentityAsync("Reviews");
            }

            // 12a) RESERVATION ADD-ONS
            var resList = reservations.ToList();
            ctx.ReservationAddOns.AddRange(
                new ReservationAddOn
                {
                    ReservationId = resList[0].Id,
                    AddOnId = 1,
                    Quantity = 4,
                    UnitPrice = 10m,
                    LineTotal = 40m
                },
                new ReservationAddOn
                {
                    ReservationId = resList[1].Id,
                    AddOnId = 2,
                    Quantity = 3,
                    UnitPrice = 8m,
                    LineTotal = 24m
                },
                new ReservationAddOn
                {
                    ReservationId = resList[2].Id,
                    AddOnId = 4,
                    Quantity = 3,
                    UnitPrice = 15m,
                    LineTotal = 45m
                },
                new ReservationAddOn
                {
                    ReservationId = resList[3].Id,
                    AddOnId = 5,
                    Quantity = 3,
                    UnitPrice = 30m,
                    LineTotal = 90m
                }
            );
            await ctx.SaveChangesAsync();
        }

        // Always align identities after seed (or existing data)
        await ResetIdentityAsync("Reservations");
        await ResetIdentityAsync("ReservationAddOns");

        // 13) PAYMENTS + PAYMENT METHODS
        if (!await ctx.PaymentMethods.AnyAsync())
        {
            ctx.PaymentMethods.Add(
                new PaymentMethod
                {
                    UserId = demoUserId,
                    StripePaymentMethodId = "pm_demo_visa",
                    StripeCustomerId = "cus_demo_123",
                    Last4 = "4242",
                    Brand = "Visa",
                    ExpMonth = 12,
                    ExpYear = 30,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                }
            );
            await ctx.SaveChangesAsync();
        }
        await ResetIdentityAsync("PaymentMethods");

        if (!await ctx.Payments.AnyAsync())
        {
            var resIds = await ctx.Reservations.Select(r => new { r.Id, r.Subtotal }).ToListAsync();
            if (resIds.Count > 0)
            {
                ctx.Payments.Add(new Payment
                {
                    ReservationId = resIds[0].Id,
                    Amount = resIds[0].Subtotal,
                    Currency = "EUR",
                    Provider = "Stripe",
                    Status = "Succeeded",
                    PaymentIntentId = "pi_demo_1",
                    ChargeId = "ch_demo_1",
                    CardBrand = "visa",
                    CardLast4 = "4242",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                });
            }
        }
        await ResetIdentityAsync("Payments");

        // 14) LOYALTY / POINTS (if you have a ledger/entity)
        // if (!await ctx.LoyaltyLedgers.AnyAsync()) {
        //     ctx.LoyaltyLedgers.Add(new LoyaltyLedger { ... });
        //     await ctx.SaveChangesAsync();
        // }

        // 15) NOTIFICATIONS (for demo reservation)
        if (!await ctx.Notifications.AnyAsync())
        {
            var demoReservationId = await ctx.Reservations.Select(r => r.Id).FirstAsync();

            ctx.Notifications.Add(
                new Notification
                {
                    UserId = demoUserId,
                    ReservationId = demoReservationId,
                    Type = "reservation_created",
                    Message = "Your demo reservation has been created.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }
            );
            await ctx.SaveChangesAsync();
        }
        await ResetIdentityAsync("Notifications");

        // Backfill totals for any existing rows with missing Total
        try
        {
            await ctx.Database.ExecuteSqlRawAsync("""
                UPDATE "Reservations"
                SET "Total" = "Subtotal" + "TaxesAndFees" + "ServiceFee"
                WHERE "Total" = 0;
            """);
        }
        catch
        {
            // ignore if the table/columns differ
        }

        // 16) REPORTS (if you have a reporting entity)
        // if (!await ctx.Reports.AnyAsync())
        // {
        //     ctx.Reports.Add(new Report { ... });
        //     await ctx.SaveChangesAsync();
        // }

        // 17) ANY OTHER DOMAIN ENTITIES
        // e.g. Facilities, HotelFacilities, StatisticsSnapshots, etc.
        // Follow the same pattern:
        // if (!await ctx.Table.AnyAsync()) { ctx.Table.AddRange(...); await ctx.SaveChangesAsync(); }
    }
}
