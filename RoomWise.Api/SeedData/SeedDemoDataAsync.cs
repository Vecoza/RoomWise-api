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
            var spa  = new Facility { Id = 3, Code = "spa",  Name = "Spa" };
            var gym  = new Facility { Id = 4, Code = "gym",  Name = "Gym" };
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

        // 6) ROOM AVAILABILITY (Nov–Dec 2025)
        if (!await ctx.RoomAvailabilities.AnyAsync())
        {
            var start = new DateTime(2025, 11, 1);
            var end   = new DateTime(2025, 12, 31);

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
                    Description = "15% off midweek stays.",
                    DiscountPercent = 15,
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
                }
            );
            await ctx.SaveChangesAsync();
            await ResetIdentityAsync("Promotions");
        }

        // 7) TAGS + HOTEL TAGS
        if (!await ctx.Tags.AnyAsync())
        {
            var tagBusiness = new Tag { Id = 1, Name = "Business" };
            var tagFamily   = new Tag { Id = 2, Name = "Family" };
            var tagSpa      = new Tag { Id = 3, Name = "Spa" };
            var tagBeach    = new Tag { Id = 4, Name = "Beach" };
            var tagSki      = new Tag { Id = 5, Name = "Ski" };

            ctx.Tags.AddRange(tagBusiness, tagFamily, tagSpa, tagBeach, tagSki);
            await ctx.SaveChangesAsync();

            ctx.HotelTags.AddRange(
                new HotelTag { HotelId = 1, TagId = 1 },
                new HotelTag { HotelId = 1, TagId = 2 },
                new HotelTag { HotelId = 2, TagId = 2 },
                new HotelTag { HotelId = 3, TagId = 4 },
                new HotelTag { HotelId = 3, TagId = 2 },
                new HotelTag { HotelId = 4, TagId = 5 },
                new HotelTag { HotelId = 4, TagId = 3 }
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
                    Url = "https://dynamic-media-cdn.tripadvisor.com/media/photo-o/2c/b0/c1/4c/boutique-hotels.jpg?w=1200&h=-1&s=1",
                    SortOrder = 1
                },
                new HotelImage
                {
                    Id = 2,
                    HotelId = 1,
                    Url = "https://media.cntraveler.com/photos/685595770556b60be007dced/16:9/w_2864,h_1611,c_limit/062325-Best-Hotels-LA-W-Hollywood-PR_MG_2125-2-copy.jpg",
                    SortOrder = 2
                },
                new HotelImage
                {
                    Id = 3,
                    HotelId = 2,
                    Url = "https://miro.medium.com/v2/1*V-1_xLadALuv6ueJsO3o_A.jpeg",
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

        // 12) DEMO RESERVATIONS (for statistics etc.)
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
                    Subtotal = 85m * 2, // 2 nights
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
                }
            };

            ctx.Reservations.AddRange(reservations);
            await ctx.SaveChangesAsync();

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
