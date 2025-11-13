using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public sealed class PhoneContactService
    : BaseCRUDService<PhoneContactResponse, PhoneContactSearchObject, PhoneContact, PhoneContactUpsertRequest, PhoneContactUpsertRequest>, IPhoneContactService
{
    public PhoneContactService(DbContext db, IMapper mapper) : base(db, mapper) { }

    protected override IQueryable<PhoneContact> ApplyFilter(IQueryable<PhoneContact> q, PhoneContactSearchObject s)
    {
        if (s.HotelId.HasValue) q = q.Where(x => x.HotelId == s.HotelId.Value);
        return q.OrderBy(x => x.Id);
    }
}