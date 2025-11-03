using dotenv.net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RoomWise.Api.Data;
using RoomWise.Api.Extensions;
using RoomWise.Model;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;
using RoomWise.Services.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

DotEnv.Load();

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddOpenApi();

builder.Services.AddTransient<IHotelService, HotelService>();
builder.Services.AddTransient<IRoomTypeService, RoomTypeService>();
builder.Services.AddTransient<IRoomRateService, RoomRateService>();



builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<DataContext>());


builder.Services.AddControllers();

builder.Services.AddAuthentication();

builder.Services.AddAuthorization();

builder.Services.AddIdentityApiEndpoints<AppUser>()
    .AddEntityFrameworkStores<DataContext>();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Servers = new List<OpenApiServer>
        {
            new OpenApiServer { Url = "http://localhost:5184" }

        };
        return Task.CompletedTask;
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
/*if (app.Environment.IsDevelopment())*/
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Theme = ScalarTheme.Alternate;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.ShowSidebar = true;
    });

    app.ApplyMigrations();

}

app.MapIdentityApi<AppUser>();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();