using HotelBooking.BLL.DTOs.Hotel;
using HotelBooking.BLL.Interfaces;
using HotelBooking.DAL.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Exceptions;
using HotelBooking.Domain.Views;

namespace HotelBooking.BLL.Services;

public sealed class HotelService : IHotelService
{
    private static readonly HashSet<string> AllowedRoomStatuses =
        ["available", "occupied", "maintenance", "inactive"];

    private readonly IHotelRepository _hotelRepository;

    public HotelService(IHotelRepository hotelRepository)
    {
        _hotelRepository = hotelRepository;
    }

    public async Task<IReadOnlyList<Hotel>> GetAllHotelsAsync(bool onlyActive = false) =>
        (await _hotelRepository.GetAllHotelsAsync(onlyActive)).ToList();

    public async Task<Hotel> GetHotelByIdAsync(int id) =>
        await _hotelRepository.GetHotelByIdAsync(id)
        ?? throw new NotFoundException($"Готель з ID={id} не знайдено.");

    public async Task<IReadOnlyList<RoomCatalogItem>> GetRoomCatalogAsync(int? hotelId = null, string? city = null, bool onlyActiveHotels = true) =>
        (await _hotelRepository.GetRoomCatalogAsync(hotelId, city, onlyActiveHotels)).ToList();

    public async Task<RoomCatalogItem> GetRoomDetailsAsync(int roomId) =>
        await _hotelRepository.GetRoomCatalogItemAsync(roomId)
        ?? throw new NotFoundException($"Номер з ID={roomId} не знайдено.");

    public async Task<IReadOnlyList<AvailableRoomItem>> SearchAvailableRoomsAsync(string? city, DateOnly checkIn, DateOnly checkOut, short guests)
    {
        if (checkOut <= checkIn)
            throw new ValidationException("Дата виїзду має бути пізнішою за дату заїзду.");

        if (guests < 1 || guests > 12)
            throw new ValidationException("Кількість гостей має бути в межах від 1 до 12.");

        return (await _hotelRepository.FindAvailableRoomsAsync(city, checkIn, checkOut, guests)).ToList();
    }

    public async Task<decimal> CalculateStayPriceAsync(int roomId, DateOnly checkIn, DateOnly checkOut)
    {
        if (checkOut <= checkIn)
            throw new ValidationException("Дата виїзду має бути пізнішою за дату заїзду.");

        var price = await _hotelRepository.CalculateStayPriceAsync(roomId, checkIn, checkOut);
        return price ?? throw new NotFoundException($"Номер з ID={roomId} не знайдено.");
    }

    public async Task<IReadOnlyList<RoomCategory>> GetCategoriesAsync() =>
        (await _hotelRepository.GetCategoriesAsync()).ToList();

    public async Task<IReadOnlyList<Amenity>> GetAmenitiesAsync() =>
        (await _hotelRepository.GetAmenitiesAsync()).ToList();

    public async Task<IReadOnlyList<string>> GetActiveCitiesAsync() =>
        (await _hotelRepository.GetActiveCitiesAsync()).ToList();

    public Task<int> CreateHotelAsync(CreateHotelDto dto)
    {
        var hotel = new Hotel
        {
            HotelCode = dto.HotelCode.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            StarRating = dto.StarRating,
            Country = dto.Country.Trim(),
            City = dto.City.Trim(),
            Address = dto.Address.Trim(),
            Phone = dto.Phone.Trim(),
            Email = dto.Email.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim(),
            IsActive = dto.IsActive
        };

        return _hotelRepository.CreateHotelAsync(hotel);
    }

    public async Task UpdateHotelAsync(UpdateHotelDto dto)
    {
        var hotel = new Hotel
        {
            Id = dto.Id,
            Name = dto.Name.Trim(),
            StarRating = dto.StarRating,
            Country = dto.Country.Trim(),
            City = dto.City.Trim(),
            Address = dto.Address.Trim(),
            Phone = dto.Phone.Trim(),
            Email = dto.Email.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim(),
            IsActive = dto.IsActive
        };

        if (!await _hotelRepository.UpdateHotelAsync(hotel))
            throw new NotFoundException($"Готель з ID={dto.Id} не знайдено.");
    }

    public async Task SetHotelActiveAsync(int id, bool isActive)
    {
        if (!await _hotelRepository.SetHotelActiveAsync(id, isActive))
            throw new NotFoundException($"Готель з ID={id} не знайдено.");
    }

    public Task<int> CreateRoomAsync(CreateRoomDto dto)
    {
        if (!AllowedRoomStatuses.Contains(dto.Status))
            throw new ValidationException($"Неприпустимий статус номера: {dto.Status}.");

        var room = new Room
        {
            HotelId = dto.HotelId,
            CategoryId = dto.CategoryId,
            RoomNumber = dto.RoomNumber.Trim(),
            Floor = dto.Floor,
            Status = dto.Status,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim()
        };

        return _hotelRepository.CreateRoomAsync(room, dto.AmenityIds);
    }

    public async Task UpdateRoomStatusAsync(int roomId, string status)
    {
        if (!AllowedRoomStatuses.Contains(status))
            throw new ValidationException($"Неприпустимий статус номера: {status}.");

        if (!await _hotelRepository.UpdateRoomStatusAsync(roomId, status))
            throw new NotFoundException($"Номер з ID={roomId} не знайдено.");
    }
}