using HotelBooking.BLL.DTOs.Hotel;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Views;
using HotelEntity = HotelBooking.Domain.Entities.Hotel;

namespace HotelBooking.Web.Models;

public sealed class AdminHotelsViewModel
{
    public IReadOnlyList<HotelEntity> Hotels { get; set; } = [];
    public IReadOnlyList<RoomCatalogItem> Rooms { get; set; } = [];
    public IReadOnlyList<RoomCategory> Categories { get; set; } = [];
    public IReadOnlyList<Amenity> Amenities { get; set; } = [];
    public CreateHotelDto NewHotel { get; set; } = new();
    public CreateRoomDto NewRoom { get; set; } = new();
}