using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Views;

namespace HotelBooking.Web.Models;

public sealed class BookingDetailsViewModel
{
    public BookingSummary Booking { get; set; } = null!;
    public IReadOnlyList<Payment> Payments { get; set; } = [];
}