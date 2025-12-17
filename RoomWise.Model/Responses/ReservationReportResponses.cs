namespace RoomWise.Model.Responses;

public class ReservationReportFilter
{
    public int? HotelId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Status { get; set; }
}

public class ReservationStatusCount
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ReservationSummaryResponse
{

    public int TotalReservations { get; set; }


    public int TotalNights { get; set; }


    public decimal TotalRevenue { get; set; }


    public List<ReservationStatusCount> StatusBreakdown { get; set; } = new();
}