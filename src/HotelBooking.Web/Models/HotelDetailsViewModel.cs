using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Views;

namespace HotelBooking.Web.Models;

public sealed class HotelDetailsViewModel
{
    public Hotel Hotel { get; set; } = null!;
    public IReadOnlyList<RoomCatalogItem> Rooms { get; set; } = [];
}