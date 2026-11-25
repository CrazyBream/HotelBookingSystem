namespace HotelBooking.BLL.DTOs.Auth;

public sealed class AuthenticatedUserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsStaff { get; set; }
    public int? HotelId { get; set; }
}