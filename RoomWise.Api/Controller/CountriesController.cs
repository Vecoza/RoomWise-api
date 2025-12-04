using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class CountriesController
    : BaseCRUDController<CountryResponse, CountrySearchObject, CountryUpsertRequest, CountryUpsertRequest>
{
    private readonly ICityService _cities;

    public CountriesController(ICountryService countries, ICityService cities) : base(countries)
    {
        _cities = cities;
    }

    // GET /api/countries/{countryId}/cities
    [HttpGet("{countryId:int}/cities")]
    public Task<PagedResult<CityResponse>> GetCities(int countryId, CancellationToken ct = default)
        => _cities.GetAsync(new CitySearchObject { CountryId = countryId, RetrieveAll = true });
}
