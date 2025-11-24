using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AutoMapper;
using dotenv.net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RoomWise.Api;
using RoomWise.Api.Auth;
using RoomWise.Api.Background;
using RoomWise.Api.Data;
using RoomWise.Api.Extensions;
using RoomWise.Api.Mapping;
using RoomWise.Api.SeedData;
using RoomWise.Model;
using RoomWise.Services.Interface;
using RoomWise.Services.Services;
using Scalar.AspNetCore;
using Stripe;
using PaymentMethodService = RoomWise.Services.Services.PaymentMethodService;
using ReviewService = RoomWise.Services.Services.ReviewService;

var builder = WebApplication.CreateBuilder(args);


DotEnv.Load();
builder.Configuration.AddEnvironmentVariables();


var stripeSection = builder.Configuration.GetSection("Stripe");
var stripeKey = stripeSection["SecretKey"];

Console.WriteLine($"Stripe:SecretKey length = {stripeKey?.Length ?? 0}");

if (string.IsNullOrWhiteSpace(stripeKey))
{
    throw new InvalidOperationException("Stripe:SecretKey is not configured!");
}

StripeConfiguration.ApiKey = stripeKey;



Console.WriteLine($"ENV: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Jwt:Key = {builder.Configuration["Jwt:Key"]}");
Console.WriteLine($"Jwt:Issuer = {builder.Configuration["Jwt:Issuer"]}");
Console.WriteLine($"Jwt:Audience = {builder.Configuration["Jwt:Audience"]}");


IdentityModelEventSource.ShowPII = true;


/*StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];*/
builder.Services.Configure<RoomWise.Api.Options.StripeOptions>(
    builder.Configuration.GetSection("Stripe"));


builder.Services.AddAutoMapper(typeof(Program).Assembly, typeof(RoomWiseProfile).Assembly);


builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<DataContext>());


builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<DataContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();


builder.Services.AddTransient<IHotelService, HotelService>();
builder.Services.AddTransient<IRoomTypeService, RoomTypeService>();
builder.Services.AddTransient<IRoomRateService, RoomRateService>();
builder.Services.AddTransient<IReservationService, ReservationService>();
builder.Services.AddTransient<IWishlistService, WishlistService>();
builder.Services.AddTransient<ISearchService, SearchService>();
builder.Services.AddTransient<IRoomAvailabilityService, RoomAvailabilityService>();
builder.Services.AddTransient<IReviewService, ReviewService>();
builder.Services.AddTransient<IPromotionService, PromotionService>();
builder.Services.AddTransient<IUserProfileService, UserProfileService>();
builder.Services.AddTransient<ILoyaltyService, LoyaltyService>();
builder.Services.AddTransient<ITagService, TagService>();
builder.Services.AddTransient<IHotelImageService, HotelImageService>();
builder.Services.AddTransient<IPhoneContactService, PhoneContactService>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddTransient<IPaymentService, PaymentService>();
builder.Services.AddTransient<IPaymentMethodService, PaymentMethodService>();
builder.Services.AddTransient<IAddOnService, AddOnService>();
builder.Services.AddTransient<IReportService, ReportService>();
builder.Services.AddTransient<IStatisticsService, StatisticsService>();


builder.Services.AddHostedService<ReservationReminderService>();

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();



JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var jwtSection = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(
    jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.")
);
var signingKey = new SymmetricSecurityKey(keyBytes);


builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
              
                if (string.IsNullOrWhiteSpace(context.Token))
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = authHeader["Bearer ".Length..].Trim().Trim('"', '\'');
                    }
                }

                Console.WriteLine($"[JWT] Raw token: '{context.Token}'");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[JWT] Auth failed: {context.Exception}");
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = signingKey,

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();


builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();

    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Servers = new List<OpenApiServer>
        {
            new() { Url = "/" }
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Theme = ScalarTheme.DeepSpace;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.ShowSidebar = true;
    });

    app.ApplyMigrations();
}
else
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Theme = ScalarTheme.DeepSpace;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.ShowSidebar = true;
    });
}


await app.SeedIdentityDataAsync();
await app.SeedLocationDataAsync();

await app.SeedDemoDataAsync();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
