

using Microsoft.EntityFrameworkCore;
using RoomWise.Api.Data;

namespace RoomWise.Api.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using DataContext context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.Database.Migrate();
    }

}