using System.ComponentModel.DataAnnotations;

namespace HotelBooking.BLL.DTOs.Booking;

public sealed class CreateBookingDto
{
    [Required]
    public int RoomId { get; set; }

    [Required]
    public DateOnly CheckInDate { get; set; }

    [Required]
    public DateOnly CheckOutDate { get; set; }

    [Range(1, 12, ErrorMessage = "Кількість гостей має бути від 1 до 12")]
    public short GuestsCount { get; set; } = 1;

    public string? SpecialRequests { get; set; }
}