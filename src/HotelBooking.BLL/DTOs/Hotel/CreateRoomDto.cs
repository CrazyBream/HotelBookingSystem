using System.ComponentModel.DataAnnotations;

namespace HotelBooking.BLL.DTOs.Hotel;

public sealed class CreateRoomDto
{
    [Required]
    public int HotelId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Введіть номер кімнати")]
    public string RoomNumber { get; set; } = string.Empty;

    [Range(0, 100)]
    public short Floor { get; set; } = 1;

    public string Status { get; set; } = "available";
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<int> AmenityIds { get; set; } = [];
}