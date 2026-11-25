using System.ComponentModel.DataAnnotations;

namespace HotelBooking.BLL.DTOs.Auth;

public sealed class LoginDto
{
    [Required(ErrorMessage = "Введіть email")]
    [EmailAddress(ErrorMessage = "Некоректний формат email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть пароль")]
    public string Password { get; set; } = string.Empty;
}