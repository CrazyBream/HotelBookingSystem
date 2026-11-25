using HotelBooking.Domain.Entities;

namespace HotelBooking.DAL.Interfaces;

public interface IGuestRepository
{
    Task<Guest?> GetByIdAsync(int id);
    Task<Guest?> GetByEmailAsync(string email);
    Task<IEnumerable<Guest>> GetAllAsync();
    Task<int> CreateAsync(Guest guest);
    Task<bool> UpdateProfileAsync(int id, string firstName, string lastName, string phone, string? documentType, string? documentNumber);
    Task<bool> UpdatePasswordHashAsync(int id, string passwordHash);
    Task<bool> SetBannedAsync(int id, bool isBanned);
}