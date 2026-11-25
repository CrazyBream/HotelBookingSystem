using System.ComponentModel.DataAnnotations;

namespace HotelBooking.BLL.DTOs.Booking;

public sealed class CreatePaymentDto
{
    [Required]
    public long BookingId { get; set; }

    [Range(0.01, 10000000, ErrorMessage = "Сума має бути більшою за 0")]
    public decimal Amount { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = "online";

    [Required]
    public string PaymentType { get; set; } = "prepayment";
}