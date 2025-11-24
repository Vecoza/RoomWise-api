using Microsoft.EntityFrameworkCore;
using RoomWise.Api.Data;
using RoomWise.Model;

namespace RoomWise.Api.SeedData;

public static class LocationSeedExtensions
{
    public static async Task SeedLocationDataAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        await context.Database.MigrateAsync();


        var country = await context.Countries
            .FirstOrDefaultAsync(c => c.Iso2 == "BA");

        if (country == null)
        {
            country = new Country
            {
                Name = "Bosnia and Herzegovina",
                Iso2 = "BA"
            };

            context.Countries.Add(country);
            await context.SaveChangesAsync();
        }

        var bosniaId = country.Id;

       
        var cityNames = new[]
        {
            "Banja Luka",
            "Bijeljina",
            "Bihać",
            "Bosanska Krupa",
            "Brčko",
            "Cazin",
            "Čapljina",
            "Derventa",
            "Doboj",
            "Goražde",
            "Gračanica",
            "Gradačac",
            "Gradiška",
            "Konjic",
            "Laktaši",
            "Livno",
            "Lukavac",
            "Ljubuški",
            "Mostar",
            "Novi Travnik",
            "Orašje",
            "Prijedor",
            "Prnjavor",
            "Sarajevo",
            "Srebrenik",
            "Stolac",
            "Široki Brijeg",
            "Trebinje",
            "Tuzla",
            "Visoko",
            "Zavidovići",
            "Zenica",
            "Zvornik",
            "Živinice"
        };

        var existingCityNames = await context.Cities
            .Where(c => c.CountryId == bosniaId)
            .Select(c => c.Name)
            .ToListAsync();

        var existingSet = new HashSet<string>(existingCityNames,
            StringComparer.OrdinalIgnoreCase);

        foreach (var name in cityNames)
        {
            if (!existingSet.Contains(name))
            {
                context.Cities.Add(new City
                {
                    CountryId = bosniaId,
                    Name = name
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
