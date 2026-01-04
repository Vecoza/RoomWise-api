namespace RoomWise.Model.Responses;

public sealed class ReservationArrivalResponse
{
    public int ReservationId { get; set; }
    public string GuestFirstName { get; set; } = string.Empty;
    public string GuestLastName { get; set; } = string.Empty;
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public int Guests { get; set; }
    public decimal RoomTotal { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime CheckIn { get; set; }
}
