namespace HotelBooking.Domain.Views;

public class HotelOccupancyStats
{
    public int HotelId { get; set; }
    public string HotelCode { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public short StarRating { get; set; }
    public long TotalRooms { get; set; }
    public long OperationalRooms { get; set; }
    public long TotalBookings { get; set; }
    public long ConfirmedBookings { get; set; }
    public long CancelledBookings { get; set; }
    public decimal AvgStayNights { get; set; }
    public decimal ExpectedRevenue { get; set; }
    public decimal CollectedRevenue { get; set; }
}