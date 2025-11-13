using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;


/*[ApiController]
[Route("[controller]")]*/
public abstract class BaseController<T,TSearch> : ControllerBase
    where T:class
    where TSearch: BaseSearchObject, new()
{
    protected readonly IService<T, TSearch> _service;

    protected BaseController(IService<T, TSearch> service)
    {
        _service = service;
    }


    [HttpGet("")]
    public virtual Task<PagedResult<T>> Get([FromQuery] TSearch? search = null)
    {
       return _service.GetAsync(search ?? new TSearch());
    }

    [HttpGet("{id:int}")]
    public virtual Task<T?> GetById(int id)
    {
        return _service.GetByIdAsync(id);
    }
}