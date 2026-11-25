namespace HotelBooking.Domain.Entities;

public class RoomCategory
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short MaxGuests { get; set; }
    public decimal AreaSqm { get; set; }
    public decimal BasePrice { get; set; }
}