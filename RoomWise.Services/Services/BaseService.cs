using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class BaseService<T, TSearch, TEntity> : IService<T, TSearch>
    where T : class 
    where TSearch : BaseSearchObject 
    where TEntity : class
{
    private readonly DbContext _context;
    protected readonly IMapper _mapper;

    public BaseService(DbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public virtual async Task<PagedResult<T>> GetAsync(TSearch search)
    {
        var query = _context.Set<TEntity>().AsQueryable();
        query = ApplyFilter(query, search);

        int? total = null;

        if (search.IncludeTotalCount) total = await query.CountAsync();

        if (!search.RetrieveAll)
        {
            var pageSize = search.PageSize ?? 10;
            var page = Math.Max(0, search.Page ?? 0);
            query = query.Skip(page * pageSize).Take(pageSize);
        }

        var list = await query.ToListAsync();

        return new PagedResult<T>
        {
            Items = list.Select(MapToResponse).ToList(),
            TotalCount = total
        };

    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        var entity = await _context.Set<TEntity>().FindAsync(id);
        return entity != null ? MapToResponse(entity) : null;
    }

    protected virtual T MapToResponse(TEntity entity) => _mapper.Map<T>(entity);
    protected virtual IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, TSearch search) => query;
    


}
