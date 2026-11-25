using HotelBooking.Domain.Views;

namespace HotelBooking.DAL.Interfaces;

public interface IAnalyticsRepository
{
    Task<IEnumerable<HotelOccupancyStats>> GetHotelOccupancyStatsAsync();
    Task<IEnumerable<BookingAuditEntry>> GetRecentAuditEntriesAsync(int limit = 50);
}