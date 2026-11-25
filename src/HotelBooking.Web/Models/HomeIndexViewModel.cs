using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Views;

namespace HotelBooking.Web.Models;

public sealed class HomeIndexViewModel
{
    public IReadOnlyList<Hotel> Hotels { get; set; } = [];
    public IReadOnlyList<string> Cities { get; set; } = [];
    public IReadOnlyList<AvailableRoomItem>? SearchResults { get; set; }
    public string? SelectedCity { get; set; }
    public DateOnly CheckIn { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
    public DateOnly CheckOut { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
    public short Guests { get; set; } = 2;
}