using System.ComponentModel.DataAnnotations;

namespace HotelBooking.BLL.DTOs.Hotel;

public sealed class CreateHotelDto
{
    [Required(ErrorMessage = "Введіть код готелю")]
    public string HotelCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть назву готелю")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Зірковість має бути від 1 до 5")]
    public short StarRating { get; set; } = 4;

    [Required(ErrorMessage = "Введіть країну")]
    public string Country { get; set; } = "Україна";

    [Required(ErrorMessage = "Введіть місто")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть адресу")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть телефон")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть email")]
    [EmailAddress(ErrorMessage = "Некоректний формат email")]
    public string Email { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}