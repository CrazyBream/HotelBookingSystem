using System.ComponentModel.DataAnnotations;

namespace HotelBooking.BLL.DTOs.Auth;

public sealed class UpdateProfileDto
{
    [Required(ErrorMessage = "Введіть ім'я")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть прізвище")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть телефон")]
    public string Phone { get; set; } = string.Empty;

    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
}