namespace HotelBooking.Domain.Entities;

public class Room
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public int CategoryId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public short Floor { get; set; }
    public string Status { get; set; } = "available";
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
}