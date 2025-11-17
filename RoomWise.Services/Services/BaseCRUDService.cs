using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public abstract class BaseCRUDService<T,TSearch, TEntity, TInsert, TUpdate>
    :BaseService<T,TSearch, TEntity>, ICRUDService<T,TSearch, TInsert, TUpdate>
    where T:class
    where TSearch:BaseSearchObject
    where TEntity:class, new()
    where TInsert:class
    where TUpdate:class
{

    protected readonly DbContext _context;
    
    public BaseCRUDService(DbContext context, IMapper mapper): base(context,mapper)
    {
        _context = context;
    }

    public virtual async Task<T> CreateAsync(TInsert request)
    {
        var entity = new TEntity();
        MapInsertToEntity(entity, request);
        _context.Set<TEntity>().Add(entity);

        await BeforeInsert(entity, request);
        await _context.SaveChangesAsync();

        return MapToResponse(entity);
    }
    
    public virtual async Task<T?> UpdateAsync(int id, TUpdate request)
    {
        var entity = await _context.Set<TEntity>().FindAsync(id);
        if (entity == null) return null;

        MapUpdateToEntity(entity, request);
        await BeforeUpdate(entity, request);

        await _context.SaveChangesAsync();
        return MapToResponse(entity);
    }



    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await _context.Set<TEntity>().FindAsync(id);
        if (entity == null) return false;

        await BeforeDelete(entity);
        _context.Set<TEntity>().Remove(entity);
        await _context.SaveChangesAsync();

        return true;
    }

    protected virtual Task BeforeInsert(TEntity entity, TInsert request) => Task.CompletedTask;
    protected virtual Task BeforeUpdate(TEntity entity, TUpdate request) => Task.CompletedTask;
    protected virtual Task BeforeDelete(TEntity entity) => Task.CompletedTask;

    protected virtual TEntity MapInsertToEntity(TEntity entity, TInsert request) => _mapper.Map(request, entity);
    protected virtual void MapUpdateToEntity(TEntity entity, TUpdate request) => _mapper.Map(request, entity);
}