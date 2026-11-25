using HotelBooking.Domain.Entities;

namespace HotelBooking.DAL.Interfaces;

public interface IStaffRepository
{
    Task<Staff?> GetByIdAsync(int id);
    Task<Staff?> GetByEmailAsync(string email);
    Task<IEnumerable<Staff>> GetAllAsync();
    Task<bool> UpdatePasswordHashAsync(int id, string passwordHash);
}