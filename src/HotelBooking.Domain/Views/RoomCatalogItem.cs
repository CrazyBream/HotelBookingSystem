namespace HotelBooking.Domain.Views;

public class RoomCatalogItem
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public short Floor { get; set; }
    public string RoomStatus { get; set; } = string.Empty;
    public string? RoomDescription { get; set; }
    public string? RoomImageUrl { get; set; }
    public int HotelId { get; set; }
    public string HotelCode { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public short StarRating { get; set; }
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool HotelIsActive { get; set; }
    public int CategoryId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public short MaxGuests { get; set; }
    public decimal AreaSqm { get; set; }
    public decimal BasePrice { get; set; }
    public string[] Amenities { get; set; } = [];
}