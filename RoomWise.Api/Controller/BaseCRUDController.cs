using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;
using RoomWise.Services.Services;

namespace RoomWise.Api.Controller;


/*[ApiController]
[Route("api/[controller]")]*/
public abstract class BaseCRUDController<T,TSearch, TInsert, TUpdate> : BaseController<T,TSearch>
    where T:class
    where TSearch : BaseSearchObject, new()
    where TUpdate:class
    where TInsert:class
{
    protected readonly ICRUDService<T, TSearch, TInsert, TUpdate> _crudService;

    protected BaseCRUDController(ICRUDService<T, TSearch, TInsert, TUpdate> service) :base(service)
    {
        _crudService = service;
    }
 
    [HttpPost]
    public virtual Task<T> Create([FromBody] TInsert request)
    {
        return _crudService.CreateAsync(request);
    }

    [HttpPut("{id:int}")]
    public virtual Task<T?> Update(int id, [FromBody] TUpdate request)
    {
        return _crudService.UpdateAsync(id, request);
    }

    [HttpDelete("{id:int}")]
    public virtual Task<bool> Delete(int id)
    {
        return _crudService.DeleteAsync(id);
    } 
}