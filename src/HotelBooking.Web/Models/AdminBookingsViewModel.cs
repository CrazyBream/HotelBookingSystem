using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Views;

namespace HotelBooking.Web.Models;

public sealed class AdminBookingsViewModel
{
    public IReadOnlyList<BookingSummary> Bookings { get; set; } = [];
    public IReadOnlyList<Hotel> Hotels { get; set; } = [];
    public string? SelectedStatus { get; set; }
    public int? SelectedHotelId { get; set; }
}