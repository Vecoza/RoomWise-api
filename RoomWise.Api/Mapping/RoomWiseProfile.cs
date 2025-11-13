using AutoMapper;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;


namespace RoomWise.Api.Mapping;

public class RoomWiseProfile : Profile
{
    public RoomWiseProfile()
    {
        // -----------------------
        // UserProfile
        // -----------------------
        CreateMap<UserProfile, UserProfileResponse>()
            .ReverseMap()
            // Upsert -> Domain: don't overwrite UserId/CreatedAt/UpdatedAt here if absent
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UserProfileUpsertRequest, UserProfile>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // -----------------------
        // Country / City
        // -----------------------
        CreateMap<Country, CountryResponse>().ReverseMap();
        CreateMap<CountryUpsertRequest, Country>();

        CreateMap<City, CityResponse>().ReverseMap();
        CreateMap<CityUpsertRequest, City>();

        // -----------------------
        // Hotel + related
        // -----------------------
        CreateMap<Hotel, HotelResponse>();
        CreateMap<HotelUpsertRequest, Hotel>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore());
        
        
        CreateMap<PhoneContact, PhoneContactResponse>().ReverseMap();
        CreateMap<PhoneContactUpsertRequest, PhoneContact>()
            .ForMember(d => d.Id, opt => opt.Ignore());

        CreateMap<HotelImage, HotelImageResponse>().ReverseMap();
        CreateMap<HotelImageUpsertRequest, HotelImage>()
            .ForMember(d => d.Id, opt => opt.Ignore());

        // -----------------------
        // Facility, Tags, Junctions
        // -----------------------
        CreateMap<Facility, FacilityResponse>().ReverseMap();
        CreateMap<FacilityUpsertRequest, Facility>();

        CreateMap<HotelFacility, HotelFacilityResponse>().ReverseMap();
        CreateMap<HotelFacilityUpsertRequest, HotelFacility>()
            // composite key should be handled by the service / OnModelCreating
            .ForMember(d => d.Hotel, o => o.Ignore())
            .ForMember(d => d.Facility, o => o.Ignore());

        CreateMap<Tag, TagResponse>().ReverseMap();
        CreateMap<TagUpsertRequest, Tag>();

        CreateMap<HotelTag, HotelTagResponse>().ReverseMap();
        CreateMap<HotelTagUpsertRequest, HotelTag>()
            .ForMember(d => d.Hotel, o => o.Ignore())
            .ForMember(d => d.Tag, o => o.Ignore());

        // -----------------------
        // RoomType + Images + Facilities
        // -----------------------
        CreateMap<RoomType, RoomTypeResponse>().ReverseMap();
        CreateMap<RoomTypeUpsertRequest, RoomType>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.RoomTypeFacilities, opt => opt.Ignore())
            .ForMember(d => d.Images, opt => opt.Ignore())
            .ForMember(d => d.Rates, opt => opt.Ignore());

        CreateMap<RoomTypeImage, RoomTypeImageResponse>().ReverseMap();
        CreateMap<RoomTypeImageUpsertRequest, RoomTypeImage>()
            .ForMember(d => d.Id, opt => opt.Ignore());

        CreateMap<RoomTypeFacility, RoomTypeFacilityResponse>().ReverseMap();
        CreateMap<RoomTypeFacilityUpsertRequest, RoomTypeFacility>()
            .ForMember(d => d.RoomType, o => o.Ignore())
            .ForMember(d => d.Facility, o => o.Ignore());

        // -----------------------
        // RoomRate
        // -----------------------
        CreateMap<RoomRate, RoomRateResponse>().ReverseMap();
        CreateMap<RoomRateRequest, RoomRate>()
            .ForMember(d => d.Id, opt => opt.Ignore());

        // -----------------------
        // RoomAvailability
        // -----------------------
        CreateMap<RoomAvailability, RoomAvailabilityResponse>().ReverseMap();
        CreateMap<RoomAvailabilityUpsertRequest, RoomAvailability>()
            .ForMember(d => d.Id, opt => opt.Ignore());
        
        
        // -----------------------
        // Reservation + AddOns + AddOn
        // -----------------------
        CreateMap<Reservation, ReservationResponse>().ReverseMap();
        CreateMap<ReservationUpsertRequest, Reservation>()
            .ForMember(d => d.Id,                  o => o.Ignore())
            .ForMember(d => d.UserId,              o => o.Ignore())  
            .ForMember(d => d.ConfirmationNumber,  o => o.Ignore())
            .ForMember(d => d.CreatedAt,           o => o.Ignore())
            .ForMember(d => d.Status,              o => o.Ignore())
            .ForMember(d => d.Payments,            o => o.Ignore())
            .ForMember(d => d.AddOns,              o => o.Ignore())
            .ForMember(d => d.CancelledAt,         o => o.Ignore());
        
        
        CreateMap<ReservationAddOn, ReservationAddOnResponse>().ReverseMap();
        CreateMap<ReservationAddOnUpsertRequest, ReservationAddOn>()
            .ForMember(d => d.Reservation, o => o.Ignore())
            .ForMember(d => d.AddOn, o => o.Ignore());

        CreateMap<AddOn, AddOnResponse>().ReverseMap();
        CreateMap<AddOnUpsertRequest, AddOn>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.ReservationAddOns, opt => opt.Ignore());

        // -----------------------
        // Payment
        // -----------------------
        CreateMap<Payment, PaymentResponse>().ReverseMap();
        CreateMap<PaymentCreateRequest, Payment>()
            .ForMember(d => d.Id,             o => o.Ignore())
            .ForMember(d => d.CreatedAt,      o => o.Ignore())
            .ForMember(d => d.Status,         o => o.Ignore())
            .ForMember(d => d.Provider,       o => o.Ignore())
            .ForMember(d => d.PaymentIntentId,o => o.Ignore())
            .ForMember(d => d.ChargeId,       o => o.Ignore());
        
        
        // -----------------------
        // Review
        // -----------------------
        CreateMap<Review, ReviewResponse>().ReverseMap();
        CreateMap<ReviewUpsertRequest, Review>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore());

        // -----------------------
        // Wishlist
        // -----------------------
        CreateMap<Wishlist, WishlistResponse>().ReverseMap();

        CreateMap<WishlistUpsertRequest, Wishlist>()
            .ForMember(d => d.Id,        o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore()); 
        // -----------------------
        // Promotion
        // -----------------------
        CreateMap<Promotion, PromotionResponse>().ReverseMap();
        CreateMap<PromotionUpsertRequest, Promotion>()
            .ForMember(d => d.Id, opt => opt.Ignore());
           
        // -----------------------
        // Notification
        // -----------------------
        CreateMap<Notification, NotificationResponse>().ReverseMap();
        CreateMap<NotificationCreateRequest, Notification>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore());

        // -----------------------
        // PaymentMethod
        // -----------------------
        CreateMap<PaymentMethod, PaymentMethodResponse>().ReverseMap();
        CreateMap<PaymentMethodUpsertRequest, PaymentMethod>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore());

        // -----------------------
        // OutboxMessage
        // -----------------------
        CreateMap<OutboxMessage, OutboxMessageResponse>().ReverseMap();
        CreateMap<OutboxMessageCreateRequest, OutboxMessage>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.ProcessedAt, opt => opt.Ignore());
        
        

        CreateMap<LoyaltyPoint, LoyaltyPointResponse>();
    }
}
