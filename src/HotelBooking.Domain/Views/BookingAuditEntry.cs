namespace HotelBooking.Domain.Views;

public class BookingAuditEntry
{
    public long Id { get; set; }
    public long BookingId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; }
}