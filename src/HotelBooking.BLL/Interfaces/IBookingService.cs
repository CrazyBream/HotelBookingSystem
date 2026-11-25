using HotelBooking.BLL.DTOs.Booking;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Views;

namespace HotelBooking.BLL.Interfaces;

public interface IBookingService
{
    Task<BookingCreatedResultDto> CreateBookingAsync(int guestId, CreateBookingDto dto);
    Task<BookingSummary> GetBookingByIdAsync(long bookingId);
    Task<IReadOnlyList<BookingSummary>> GetGuestBookingsAsync(int guestId);
    Task<IReadOnlyList<BookingSummary>> GetAllBookingsAsync(string? status = null, int? hotelId = null);
    Task ConfirmBookingAsync(long bookingId);
    Task CancelBookingAsync(long bookingId, int? requestingGuestId = null);
    Task MarkNoShowAsync(long bookingId);
    Task<long> CheckInGuestAsync(CheckInDto dto, int staffId);
    Task CheckOutGuestAsync(long bookingId, int staffId, string? notes);
    Task<long> AddPaymentAsync(CreatePaymentDto dto, int? staffId);
    Task<IReadOnlyList<Payment>> GetPaymentsAsync(long bookingId);
}