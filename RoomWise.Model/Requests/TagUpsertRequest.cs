using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class TagUpsertRequest
{
    [Required, MaxLength(60)]
    public string Name { get; set; } = "";
}