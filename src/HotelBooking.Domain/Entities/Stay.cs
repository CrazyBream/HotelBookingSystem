namespace HotelBooking.Domain.Entities;

public class Stay
{
    public long Id { get; set; }
    public long BookingId { get; set; }
    public int CheckedInBy { get; set; }
    public int? CheckedOutBy { get; set; }
    public DateTime ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }
    public string? RoomKeyCard { get; set; }
    public string? Notes { get; set; }
}