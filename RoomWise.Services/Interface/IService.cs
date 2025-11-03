using RoomWise.Model.Responses;

namespace RoomWise.Services.Interface;

public interface IService<T,TSearch> where T :class where TSearch :class
{
    Task<PagedResult<T>> GetAsync(TSearch search);

    Task<T?> GetByIdAsync(int id);
}