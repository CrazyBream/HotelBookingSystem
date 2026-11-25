namespace HotelBooking.BLL.DTOs.Booking;

public sealed class BookingCreatedResultDto
{
    public long BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
}