using HotelBooking.Domain.Views;

namespace HotelBooking.Web.Models;

public sealed class RoomDetailsViewModel
{
    public RoomCatalogItem Room { get; set; } = null!;
    public DateOnly CheckIn { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
    public DateOnly CheckOut { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
    public short GuestsCount { get; set; } = 1;
    public decimal EstimatedPrice { get; set; }
}