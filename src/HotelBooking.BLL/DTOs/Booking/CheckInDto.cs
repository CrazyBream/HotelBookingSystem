namespace HotelBooking.BLL.DTOs.Booking;

public sealed class CheckInDto
{
    public long BookingId { get; set; }
    public string? RoomKeyCard { get; set; }
    public string? Notes { get; set; }
}