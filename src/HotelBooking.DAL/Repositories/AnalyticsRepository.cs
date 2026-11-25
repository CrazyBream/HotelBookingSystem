using Dapper;
using HotelBooking.DAL.Infrastructure;
using HotelBooking.DAL.Interfaces;
using HotelBooking.Domain.Views;

namespace HotelBooking.DAL.Repositories;

public sealed class AnalyticsRepository : BaseRepository, IAnalyticsRepository
{
    public AnalyticsRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task<IEnumerable<HotelOccupancyStats>> GetHotelOccupancyStatsAsync() =>
        ExecuteAsync(conn => conn.QueryAsync<HotelOccupancyStats>(
            """
            SELECT *
            FROM hotel.v_hotel_occupancy_stats
            ORDER BY collected_revenue DESC, hotel_id ASC
            """));

    public Task<IEnumerable<BookingAuditEntry>> GetRecentAuditEntriesAsync(int limit = 50) =>
        ExecuteAsync(conn => conn.QueryAsync<BookingAuditEntry>(
            """
            SELECT id, booking_id, old_status, new_status, changed_by, reason, changed_at
            FROM hotel.booking_audit
            ORDER BY changed_at DESC, id DESC
            LIMIT @Limit
            """,
            new { Limit = limit }));
}