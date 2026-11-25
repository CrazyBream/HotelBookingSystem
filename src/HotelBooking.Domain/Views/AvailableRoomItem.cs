namespace HotelBooking.Domain.Views;

public class AvailableRoomItem
{
    public int RoomId { get; set; }
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public short StarRating { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public short MaxGuests { get; set; }
    public decimal BasePrice { get; set; }
    public decimal TotalStayPrice { get; set; }
}