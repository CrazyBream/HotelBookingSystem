using HotelBooking.BLL.DTOs.Hotel;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Views;

namespace HotelBooking.BLL.Interfaces;

public interface IHotelService
{
    Task<IReadOnlyList<Hotel>> GetAllHotelsAsync(bool onlyActive = false);
    Task<Hotel> GetHotelByIdAsync(int id);
    Task<IReadOnlyList<RoomCatalogItem>> GetRoomCatalogAsync(int? hotelId = null, string? city = null, bool onlyActiveHotels = true);
    Task<RoomCatalogItem> GetRoomDetailsAsync(int roomId);
    Task<IReadOnlyList<AvailableRoomItem>> SearchAvailableRoomsAsync(string? city, DateOnly checkIn, DateOnly checkOut, short guests);
    Task<decimal> CalculateStayPriceAsync(int roomId, DateOnly checkIn, DateOnly checkOut);
    Task<IReadOnlyList<RoomCategory>> GetCategoriesAsync();
    Task<IReadOnlyList<Amenity>> GetAmenitiesAsync();
    Task<IReadOnlyList<string>> GetActiveCitiesAsync();
    Task<int> CreateHotelAsync(CreateHotelDto dto);
    Task UpdateHotelAsync(UpdateHotelDto dto);
    Task SetHotelActiveAsync(int id, bool isActive);
    Task<int> CreateRoomAsync(CreateRoomDto dto);
    Task UpdateRoomStatusAsync(int roomId, string status);
}