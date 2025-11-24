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
                }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("Hotels");
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
                }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("RoomTypes");
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
                    Price = 85m,
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
                    Price = 75m,
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
                }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("AddOns");
        }

        // 5) ROOM AVAILABILITY (10–20 Nov 2025)
        if (!await ctx.RoomAvailabilities.AnyAsync())
        {
            var start = new DateTime(2025, 11, 10);
            var end = new DateTime(2025, 11, 20);

            var roomTypeIds = await ctx.RoomTypes.Select(rt => rt.Id).ToListAsync();
            var rows = new List<RoomAvailability>();

            foreach (var rtId in roomTypeIds)
            {
                for (var d = start; d < end; d = d.AddDays(1))
                {
                    rows.Add(new RoomAvailability
                    {
                        RoomTypeId = rtId,
                        Date = d,
                        Available = 10
                    });
                }
            }

            await ctx.RoomAvailabilities.AddRangeAsync(rows);
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("RoomAvailabilities");
        }

        // 6) PROMOTIONS
        if (!await ctx.Promotions.AnyAsync())
        {
            ctx.Promotions.Add(
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

            ctx.Tags.AddRange(tagBusiness, tagFamily, tagSpa);
            await ctx.SaveChangesAsync();

            ctx.HotelTags.AddRange(
                new HotelTag { HotelId = 1, TagId = 1 },
                new HotelTag { HotelId = 1, TagId = 2 },
                new HotelTag { HotelId = 2, TagId = 2 }
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
                    Url = "https://example.com/hotels/1/main.jpg",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 2,
                    HotelId = 1,
                    Url = "https://example.com/hotels/1/lobby.jpg",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 3,
                    HotelId = 2,
                    Url = "https://example.com/hotels/2/main.jpg",
                    SortOrder = 1
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

        // 11) REVIEWS
        if (!await ctx.Reviews.AnyAsync())
        {
            ctx.Reviews.Add(
                new Review
                {
                    Id = 1,
                    HotelId = 1,
                    UserId = demoUserId,
                    Rating = 5,
                    Title = "Great stay!",
                    Body = "Perfect location and friendly staff.",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("Reviews");
        }

        // 12) SIMPLE DEMO RESERVATION (for statistics etc.)
        if (!await ctx.Reservations.AnyAsync())
        {
            var res = new Reservation
            {
                PublicId = Guid.NewGuid(),
                UserId = demoUserId,
                HotelId = 1,
                RoomTypeId = 1,
                CheckIn = new DateTime(2025, 11, 10),
                CheckOut = new DateTime(2025, 11, 12),
                Guests = 2,
                Subtotal = 85m * 2, // 2 nights
                Currency = "EUR",
                Status = "Confirmed",
                ConfirmationNumber = "RW-DEMO-0001",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            ctx.Reservations.Add(res);
            await ctx.SaveChangesAsync();

            // 12a) RESERVATION ADD-ONS
            ctx.ReservationAddOns.Add(
                new ReservationAddOn
                {
                    ReservationId = res.Id,
                    AddOnId = 1,   // breakfast
                    Quantity = 2 * 2, // 2 guests * 2 nights
                    UnitPrice = 10m,
                    LineTotal = 40m
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

        // Ensure Payments identity is aligned even if no seed row
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
