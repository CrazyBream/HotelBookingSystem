using HotelBooking.BLL.DTOs.Auth;
using HotelBooking.Domain.Views;

namespace HotelBooking.Web.Models;

public sealed class ProfileViewModel
{
    public UpdateProfileDto Profile { get; set; } = new();
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<BookingSummary> Bookings { get; set; } = [];
}