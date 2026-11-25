namespace HotelBooking.Domain.Entities;

public class Payment
{
    public long Id { get; set; }
    public long BookingId { get; set; }
    public int? ProcessedBy { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentType { get; set; } = "prepayment";
    public string Status { get; set; } = "completed";
    public string? TransactionRef { get; set; }
    public DateTime PaidAt { get; set; }
}