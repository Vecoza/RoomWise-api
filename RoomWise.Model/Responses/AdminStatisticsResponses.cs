namespace RoomWise.Model.Responses;

public class AdminStatsOverviewResponse
{
    public decimal TotalRevenue { get; set; }
    public int TotalReservations { get; set; }
    public int TotalUsers { get; set; }
    public double AvgStayLengthNights { get; set; }
    public double OccupancyRateLast30Days { get; set; }
}

public class RevenueByMonthItem
{
    public int Month { get; set; }       
    public decimal Revenue { get; set; } 
}

public class HotelStatsItem
{
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;

    public int ReservationsCount { get; set; }
    public decimal Revenue { get; set; }
    public double Rating { get; set; }
}

public class UserStatsItem
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }

    public int ReservationsCount { get; set; }
    public decimal Revenue { get; set; }
    public int Nights { get; set; }
}