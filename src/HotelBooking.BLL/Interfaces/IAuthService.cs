using HotelBooking.BLL.DTOs.Auth;
using HotelBooking.Domain.Entities;

namespace HotelBooking.BLL.Interfaces;

public interface IAuthService
{
    Task<AuthenticatedUserDto> LoginAsync(LoginDto dto);
    Task<AuthenticatedUserDto> RegisterGuestAsync(RegisterDto dto);
    Task<Guest> GetGuestProfileAsync(int guestId);
    Task UpdateGuestProfileAsync(int guestId, UpdateProfileDto dto);
    Task EnsureSeedPasswordsAsync(string defaultPassword);
}