using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;

namespace RoomWise.Services.Interface;

public interface ICountryService
    : ICRUDService<CountryResponse, CountrySearchObject, CountryUpsertRequest, CountryUpsertRequest>
{
}
