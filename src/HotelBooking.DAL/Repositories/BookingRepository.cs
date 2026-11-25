using Dapper;
using HotelBooking.DAL.Infrastructure;
using HotelBooking.DAL.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Views;

namespace HotelBooking.DAL.Repositories;

public sealed class BookingRepository : BaseRepository, IBookingRepository
{
    private sealed class CreateBookingProcResult
    {
        public long PBookingId { get; set; }
        public string PBookingCode { get; set; } = string.Empty;
        public decimal PTotalPrice { get; set; }
    }

    public BookingRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task<(long BookingId, string BookingCode, decimal TotalPrice)> CreateBookingAsync(
        int guestId, int roomId, DateOnly checkIn, DateOnly checkOut, short guestsCount, string? specialRequests) =>
        ExecuteAsync(async conn =>
        {
            var result = await conn.QuerySingleAsync<CreateBookingProcResult>(
                """
                CALL hotel.sp_create_booking(
                    @GuestId, @RoomId, @CheckIn, @CheckOut, @GuestsCount, @SpecialRequests,
                    NULL::bigint, NULL::text, NULL::numeric
                )
                """,
                new
                {
                    GuestId = guestId,
                    RoomId = roomId,
                    CheckIn = checkIn,
                    CheckOut = checkOut,
                    GuestsCount = guestsCount,
                    SpecialRequests = string.IsNullOrWhiteSpace(specialRequests) ? null : specialRequests.Trim()
                });

            return (result.PBookingId, result.PBookingCode, result.PTotalPrice);
        });

    public Task<BookingSummary?> GetSummaryByIdAsync(long bookingId) =>
        ExecuteAsync(conn => conn.QuerySingleOrDefaultAsync<BookingSummary>(
            """
            SELECT *
            FROM hotel.v_booking_summary
            WHERE booking_id = @BookingId
            """,
            new { BookingId = bookingId }));

    public Task<IEnumerable<BookingSummary>> GetSummariesByGuestAsync(int guestId) =>
        ExecuteAsync(conn => conn.QueryAsync<BookingSummary>(
            """
            SELECT *
            FROM hotel.v_booking_summary
            WHERE guest_id = @GuestId
            ORDER BY check_in_date DESC, booking_id DESC
            """,
            new { GuestId = guestId }));

    public Task<IEnumerable<BookingSummary>> GetAllSummariesAsync(string? status = null, int? hotelId = null) =>
        ExecuteAsync(conn => conn.QueryAsync<BookingSummary>(
            """
            SELECT *
            FROM hotel.v_booking_summary
            WHERE (@Status IS NULL OR btrim(@Status) = '' OR status = @Status)
              AND (@HotelId IS NULL OR hotel_id = @HotelId)
            ORDER BY created_at DESC, booking_id DESC
            """,
            new { Status = status, HotelId = hotelId }));

    public Task<bool> UpdateStatusAsync(long bookingId, string newStatus) =>
        ExecuteAsync(async conn =>
        {
            var affected = await conn.ExecuteAsync(
                """
                UPDATE hotel.bookings
                SET status = @NewStatus
                WHERE id = @BookingId
                """,
                new { BookingId = bookingId, NewStatus = newStatus });
            return affected > 0;
        });

    public Task<long> CheckInAsync(long bookingId, int staffId, string? roomKeyCard, string? notes) =>
        ExecuteAsync(conn => conn.QuerySingleAsync<long>(
            """
            CALL hotel.sp_check_in_guest(
                @BookingId, @StaffId, @RoomKeyCard, @Notes, NULL::bigint
            )
            """,
            new
            {
                BookingId = bookingId,
                StaffId = staffId,
                RoomKeyCard = string.IsNullOrWhiteSpace(roomKeyCard) ? null : roomKeyCard.Trim(),
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            }));

    public Task CheckOutAsync(long bookingId, int staffId, string? notes) =>
        ExecuteAsync(conn => conn.ExecuteAsync(
            """
            CALL hotel.sp_check_out_guest(@BookingId, @StaffId, @Notes)
            """,
            new
            {
                BookingId = bookingId,
                StaffId = staffId,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            }));

    public Task<long> AddPaymentAsync(Payment payment) =>
        ExecuteAsync(conn => conn.QuerySingleAsync<long>(
            """
            INSERT INTO hotel.payments (
                booking_id, processed_by, amount, payment_method, payment_type, status, transaction_ref
            )
            VALUES (
                @BookingId, @ProcessedBy, @Amount, @PaymentMethod, @PaymentType, @Status, @TransactionRef
            )
            RETURNING id
            """,
            payment));

    public Task<IEnumerable<Payment>> GetPaymentsByBookingAsync(long bookingId) =>
        ExecuteAsync(conn => conn.QueryAsync<Payment>(
            """
            SELECT id, booking_id, processed_by, amount, payment_method,
                   payment_type, status, transaction_ref, paid_at
            FROM hotel.payments
            WHERE booking_id = @BookingId
            ORDER BY paid_at DESC, id DESC
            """,
            new { BookingId = bookingId }));
}