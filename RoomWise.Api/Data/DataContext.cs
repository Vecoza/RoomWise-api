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
    public DbSet<LoyaltyPoint> LoyaltyPoints { get; set; }
    public DbSet<HotelAdmin> HotelAdmins { get; set; }
    public DbSet<EmailVerification> EmailVerifications { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ---------------------------
        // Identity ↔ UserProfile (1:1)
        // ---------------------------
        builder.Entity<AppUser>()
            .HasOne(a => a.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfile>(p => p.UserId);

        builder.Entity<UserProfile>(e =>
        {
            e.Property(p => p.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(p => p.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ---------------------------
        // LoyaltyPoint
        // ---------------------------
        builder.Entity<LoyaltyPoint>(e =>
        {
            e.ToTable("LoyaltyPoints");
            e.HasIndex(x => x.UserId);
        });

        // ---------------------------
        // Country / City
        // ---------------------------
        builder.Entity<City>()
            .HasOne(c => c.Country)
            .WithMany(cn => cn.Cities)
            .HasForeignKey(c => c.CountryId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---------------------------
        // Hotel and related
        // ---------------------------
        builder.Entity<Hotel>()
            .HasOne(h => h.City)
            .WithMany(c => c.Hotels)
            .HasForeignKey(h => h.CityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<HotelImage>()
            .HasOne(hi => hi.Hotel)
            .WithMany(h => h.Images)
            .HasForeignKey(hi => hi.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<HotelImage>()
            .HasIndex(x => new { x.HotelId, x.SortOrder });

        builder.Entity<PhoneContact>()
            .HasOne(pc => pc.Hotel)
            .WithMany(h => h.PhoneContacts)
            .HasForeignKey(pc => pc.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        // Facilities (HotelFacility, RoomTypeFacility)
        builder.Entity<HotelFacility>()
            .HasKey(hf => new { hf.HotelId, hf.FacilityId });

        builder.Entity<HotelFacility>()
            .HasOne(hf => hf.Hotel)
            .WithMany(h => h.HotelFacilities)
            .HasForeignKey(hf => hf.HotelId);

        builder.Entity<HotelFacility>()
            .HasOne(hf => hf.Facility)
            .WithMany(f => f.HotelFacilities)
            .HasForeignKey(hf => hf.FacilityId);

        builder.Entity<RoomTypeFacility>()
            .HasKey(rf => new { rf.RoomTypeId, rf.FacilityId });

        builder.Entity<RoomTypeFacility>()
            .HasOne(rf => rf.RoomType)
            .WithMany(rt => rt.RoomTypeFacilities)
            .HasForeignKey(rf => rf.RoomTypeId);

        builder.Entity<RoomTypeFacility>()
            .HasOne(rf => rf.Facility)
            .WithMany(f => f.RoomTypeFacilities)
            .HasForeignKey(rf => rf.FacilityId);

        // Tags (HotelTag) + unique tag name
        builder.Entity<HotelTag>()
            .HasKey(ht => new { ht.HotelId, ht.TagId });

        builder.Entity<HotelTag>()
            .HasOne(ht => ht.Hotel)
            .WithMany(h => h.HotelTags)
            .HasForeignKey(ht => ht.HotelId);

        builder.Entity<HotelTag>()
            .HasOne(ht => ht.Tag)
            .WithMany(t => t.HotelTags)
            .HasForeignKey(ht => ht.TagId);

        builder.Entity<Tag>()
            .HasIndex(t => t.Name)
            .IsUnique();

        // Wishlist (Id PK + unique pair)
        builder.Entity<Wishlist>()
            .HasIndex(w => new { w.UserId, w.HotelId })
            .IsUnique();

        builder.Entity<Wishlist>()
            .Property(w => w.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Entity<Wishlist>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId);

        // HotelAdmin (1 admin per hotel)
        builder.Entity<HotelAdmin>()
            .HasOne(ha => ha.Hotel)
            .WithMany()
            .HasForeignKey(ha => ha.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<HotelAdmin>()
            .HasOne(ha => ha.User)
            .WithMany()
            .HasForeignKey(ha => ha.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<HotelAdmin>()
            .HasIndex(ha => ha.HotelId)
            .IsUnique();

        builder.Entity<Wishlist>()
            .HasOne(w => w.Hotel)
            .WithMany()
            .HasForeignKey(w => w.HotelId);

        // ---------------------------
        // RoomType and related
        // ---------------------------
        builder.Entity<RoomType>()
            .HasOne(rt => rt.Hotel)
            .WithMany(h => h.RoomTypes)
            .HasForeignKey(rt => rt.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RoomTypeImage>()
            .HasOne(rti => rti.RoomType)
            .WithMany(rt => rt.Images)
            .HasForeignKey(rti => rti.RoomTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RoomRate>()
            .HasOne(rr => rr.RoomType)
            .WithMany(rt => rt.Rates)
            .HasForeignKey(rr => rr.RoomTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RoomAvailability>()
            .HasOne(ra => ra.RoomType)
            .WithMany(rt => rt.Availabilities)
            .HasForeignKey(ra => ra.RoomTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RoomAvailability>()
            .HasIndex(a => new { a.RoomTypeId, a.Date })
            .IsUnique();

        // ---------------------------
        // Reservation + AddOns
        // ---------------------------
        builder.Entity<Reservation>()
            .HasIndex(r => r.PublicId)
            .IsUnique();

        builder.Entity<Reservation>()
            .HasIndex(r => r.ConfirmationNumber)
            .IsUnique();

        builder.Entity<Reservation>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId);

        // Use Restrict to preserve reservation history if Hotel/RoomType is removed
        builder.Entity<Reservation>()
            .HasOne(r => r.Hotel)
            .WithMany()
            .HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Reservation>()
            .HasOne(r => r.RoomType)
            .WithMany()
            .HasForeignKey(r => r.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional promotion on reservation
        builder.Entity<Reservation>()
            .HasOne(r => r.Promotion)
            .WithMany()
            .HasForeignKey(r => r.PromotionId)
            .OnDelete(DeleteBehavior.SetNull);

        // ReservationAddOn (composite key)
        builder.Entity<ReservationAddOn>(e =>
        {
            e.HasOne(ra => ra.Reservation)
                .WithMany(r => r.AddOns)
                .HasForeignKey(ra => ra.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ra => ra.AddOn)
                .WithMany(a => a.ReservationAddOns)
                .HasForeignKey(ra => ra.AddOnId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate AddOn per Reservation
            e.HasIndex(ra => new { ra.ReservationId, ra.AddOnId })
                .IsUnique();
        });




        // AddOn → Hotel
        builder.Entity<AddOn>(e =>
        {
            e.HasOne(a => a.Hotel)
                .WithMany(h => h.AddOns)
                .HasForeignKey(a => a.HotelId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Property(a => a.Currency)
                .HasMaxLength(3);
        });
        // ---------------------------
        // Payments
        // ---------------------------
        builder.Entity<Payment>()
            .HasOne(p => p.Reservation)
            .WithMany(r => r.Payments)
            .HasForeignKey(p => p.ReservationId);

        builder.Entity<PaymentMethod>(e =>
        {
            e.HasOne(pm => pm.User)
                .WithMany()
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------------------
        // Reviews
        // ---------------------------
        builder.Entity<Review>()
            .HasOne(r => r.Hotel)
            .WithMany(h => h.Reviews)
            .HasForeignKey(r => r.HotelId);

        builder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId);


        builder.Entity<Review>()
            .HasIndex(r => new { r.ReservationId, r.UserId })
            .IsUnique()
            .HasDatabaseName("IX_Reviews_ReservationId_UserId");

        // Keep this index for listing newest per hotel
        builder.Entity<Review>()
            .HasIndex(r => new { r.HotelId, r.CreatedAt });

        // ---------------------------
        // Notifications
        // ---------------------------
        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId);

        builder.Entity<Notification>()
            .HasOne(n => n.Reservation)
            .WithMany()
            .HasForeignKey(n => n.ReservationId);

        // ---------------------------
        // Email Verification
        // ---------------------------
        builder.Entity<EmailVerification>()
            .HasOne(ev => ev.User)
            .WithMany()
            .HasForeignKey(ev => ev.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EmailVerification>()
            .HasIndex(ev => new { ev.UserId, ev.Email });

        // ---------------------------
        // Promotions
        // ---------------------------
        builder.Entity<Promotion>()
            .HasOne(p => p.Hotel)
            .WithMany()
            .HasForeignKey(p => p.HotelId)
            .OnDelete(DeleteBehavior.SetNull);
    }





}
