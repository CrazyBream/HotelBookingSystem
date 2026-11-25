using HotelBooking.Domain.Views;

namespace HotelBooking.BLL.DTOs.Admin;

public sealed class AdminDashboardDto
{
    public IReadOnlyList<HotelOccupancyStats> HotelStats { get; set; } = [];
    public IReadOnlyList<BookingAuditEntry> RecentAudits { get; set; } = [];
    public decimal TotalCollectedRevenue { get; set; }
    public decimal TotalExpectedRevenue { get; set; }
    public long TotalBookings { get; set; }
    public long TotalOperationalRooms { get; set; }
}