using HotelBooking.BLL.DTOs.Admin;
using HotelBooking.Domain.Entities;

namespace HotelBooking.BLL.Interfaces;

public interface IAdminService
{
    Task<AdminDashboardDto> GetDashboardAsync();
    Task<IReadOnlyList<Guest>> GetAllGuestsAsync();
    Task SetGuestBannedAsync(int guestId, bool isBanned);
    Task<IReadOnlyList<Staff>> GetAllStaffAsync();
}