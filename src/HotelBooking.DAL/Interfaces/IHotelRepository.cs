using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Views;

namespace HotelBooking.DAL.Interfaces;

public interface IHotelRepository
{
    Task<IEnumerable<Hotel>> GetAllHotelsAsync(bool onlyActive = false);
    Task<Hotel?> GetHotelByIdAsync(int id);
    Task<IEnumerable<RoomCatalogItem>> GetRoomCatalogAsync(int? hotelId = null, string? city = null, bool onlyActiveHotels = true);
    Task<RoomCatalogItem?> GetRoomCatalogItemAsync(int roomId);
    Task<IEnumerable<AvailableRoomItem>> FindAvailableRoomsAsync(string? city, DateOnly checkIn, DateOnly checkOut, short guests);
    Task<decimal?> CalculateStayPriceAsync(int roomId, DateOnly checkIn, DateOnly checkOut);
    Task<IEnumerable<RoomCategory>> GetCategoriesAsync();
    Task<IEnumerable<Amenity>> GetAmenitiesAsync();
    Task<IEnumerable<string>> GetActiveCitiesAsync();
    Task<int> CreateHotelAsync(Hotel hotel);
    Task<bool> UpdateHotelAsync(Hotel hotel);
    Task<bool> SetHotelActiveAsync(int id, bool isActive);
    Task<int> CreateRoomAsync(Room room, IEnumerable<int> amenityIds);
    Task<bool> UpdateRoomStatusAsync(int roomId, string status);
}