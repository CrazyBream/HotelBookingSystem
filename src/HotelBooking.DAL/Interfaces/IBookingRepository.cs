using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Views;

namespace HotelBooking.DAL.Interfaces;

public interface IBookingRepository
{
    Task<(long BookingId, string BookingCode, decimal TotalPrice)> CreateBookingAsync(
        int guestId, int roomId, DateOnly checkIn, DateOnly checkOut, short guestsCount, string? specialRequests);
    Task<BookingSummary?> GetSummaryByIdAsync(long bookingId);
    Task<IEnumerable<BookingSummary>> GetSummariesByGuestAsync(int guestId);
    Task<IEnumerable<BookingSummary>> GetAllSummariesAsync(string? status = null, int? hotelId = null);
    Task<bool> UpdateStatusAsync(long bookingId, string newStatus);
    Task<long> CheckInAsync(long bookingId, int staffId, string? roomKeyCard, string? notes);
    Task CheckOutAsync(long bookingId, int staffId, string? notes);
    Task<long> AddPaymentAsync(Payment payment);
    Task<IEnumerable<Payment>> GetPaymentsByBookingAsync(long bookingId);
}