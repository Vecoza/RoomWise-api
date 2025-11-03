using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;

namespace RoomWise.Api.Data;



public class DataContext : IdentityDbContext<AppUser>
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {

    }

    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<RoomType> RoomTypes { get; set; }
    public DbSet<RoomRate> RoomRates { get; set; }
    public DbSet<AddOn> AddOns { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<Facility> Facilities { get; set; }
    public DbSet<HotelFacility> HotelFacilities { get; set; }
    public DbSet<HotelImage> HotelImages { get; set; }
    public DbSet<HotelTag> HotelTags { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<PhoneContact> PhoneContacts { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ReservationAddOn> ReservationAddOns { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<RoomAvailability> RoomAvailabilities { get; set; }
    public DbSet<RoomTypeFacility> RoomTypeFacilities { get; set; }
    public DbSet<RoomTypeImage> RoomTypeImages { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }

}