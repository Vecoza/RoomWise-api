using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class HotelImageReorderRequest
{
    [Required]
    public List<HotelImageReorderItem> Items { get; set; } = new();

    public class HotelImageReorderItem
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int SortOrder { get; set; }
    }

}
