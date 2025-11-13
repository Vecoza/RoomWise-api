namespace RoomWise.Model.Requests;

public class HotelSearchRequest
{
    public int? CityId { get; set; }
    public string? Q { get; set; }                 
    public DateTime CheckIn { get; set; }         
    public DateTime CheckOut { get; set; }         
    public int Guests { get; set; } = 1;          
    public decimal? MaxPrice { get; set; }         
    
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? Sort { get; set; }   
}