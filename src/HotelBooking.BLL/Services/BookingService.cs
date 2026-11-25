using HotelBooking.BLL.DTOs.Booking;
using HotelBooking.BLL.Interfaces;
using HotelBooking.DAL.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Exceptions;
using HotelBooking.Domain.Views;

namespace HotelBooking.BLL.Services;

public sealed class BookingService : IBookingService
{
    private static readonly HashSet<string> AllowedPaymentMethods =
        ["card", "cash", "bank_transfer", "online"];

    private static readonly HashSet<string> AllowedPaymentTypes =
        ["prepayment", "final", "refund"];

    private readonly IBookingRepository _bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<BookingCreatedResultDto> CreateBookingAsync(int guestId, CreateBookingDto dto)
    {
        if (dto.CheckOutDate <= dto.CheckInDate)
            throw new ValidationException("Дата виїзду має бути пізнішою за дату заїзду.");

        if (dto.GuestsCount < 1)
            throw new ValidationException("Кількість гостей має бути більшою за 0.");

        var (bookingId, bookingCode, totalPrice) = await _bookingRepository.CreateBookingAsync(
            guestId,
            dto.RoomId,
            dto.CheckInDate,
            dto.CheckOutDate,
            dto.GuestsCount,
            dto.SpecialRequests);

        return new BookingCreatedResultDto
        {
            BookingId = bookingId,
            BookingCode = bookingCode,
            TotalPrice = totalPrice
        };
    }

    public async Task<BookingSummary> GetBookingByIdAsync(long bookingId) =>
        await _bookingRepository.GetSummaryByIdAsync(bookingId)
        ?? throw new NotFoundException($"Бронювання з ID={bookingId} не знайдено.");

    public async Task<IReadOnlyList<BookingSummary>> GetGuestBookingsAsync(int guestId) =>
        (await _bookingRepository.GetSummariesByGuestAsync(guestId)).ToList();

    public async Task<IReadOnlyList<BookingSummary>> GetAllBookingsAsync(string? status = null, int? hotelId = null) =>
        (await _bookingRepository.GetAllSummariesAsync(status, hotelId)).ToList();

    public async Task ConfirmBookingAsync(long bookingId)
    {
        if (!await _bookingRepository.UpdateStatusAsync(bookingId, "confirmed"))
            throw new NotFoundException($"Бронювання з ID={bookingId} не знайдено.");
    }

    public async Task CancelBookingAsync(long bookingId, int? requestingGuestId = null)
    {
        if (requestingGuestId.HasValue)
        {
            var booking = await GetBookingByIdAsync(bookingId);
            if (booking.GuestId != requestingGuestId.Value)
                throw new ValidationException("Ви можете скасовувати лише власні бронювання.");
        }

        if (!await _bookingRepository.UpdateStatusAsync(bookingId, "cancelled"))
            throw new NotFoundException($"Бронювання з ID={bookingId} не знайдено.");
    }

    public async Task MarkNoShowAsync(long bookingId)
    {
        if (!await _bookingRepository.UpdateStatusAsync(bookingId, "no_show"))
            throw new NotFoundException($"Бронювання з ID={bookingId} не знайдено.");
    }

    public Task<long> CheckInGuestAsync(CheckInDto dto, int staffId) =>
        _bookingRepository.CheckInAsync(dto.BookingId, staffId, dto.RoomKeyCard, dto.Notes);

    public Task CheckOutGuestAsync(long bookingId, int staffId, string? notes) =>
        _bookingRepository.CheckOutAsync(bookingId, staffId, notes);

    public async Task<long> AddPaymentAsync(CreatePaymentDto dto, int? staffId)
    {
        if (dto.Amount <= 0)
            throw new ValidationException("Сума платежу має бути більшою за 0.");

        if (!AllowedPaymentMethods.Contains(dto.PaymentMethod))
            throw new ValidationException("Неприпустимий спосіб оплати.");

        if (!AllowedPaymentTypes.Contains(dto.PaymentType))
            throw new ValidationException("Неприпустимий тип платежу.");

        var booking = await GetBookingByIdAsync(dto.BookingId);

        if (dto.PaymentType != "refund" && booking.Status is "cancelled" or "no_show")
            throw new ValidationException("Неможливо оплатити скасоване бронювання.");

        var payment = new Payment
        {
            BookingId = dto.BookingId,
            ProcessedBy = staffId,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            PaymentType = dto.PaymentType,
            Status = "completed",
            TransactionRef = $"WEB-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}"
        };

        var paymentId = await _bookingRepository.AddPaymentAsync(payment);

        if (booking.Status == "pending" && dto.PaymentType is "prepayment" or "final")
            await _bookingRepository.UpdateStatusAsync(dto.BookingId, "confirmed");

        return paymentId;
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsAsync(long bookingId) =>
        (await _bookingRepository.GetPaymentsByBookingAsync(bookingId)).ToList();
}