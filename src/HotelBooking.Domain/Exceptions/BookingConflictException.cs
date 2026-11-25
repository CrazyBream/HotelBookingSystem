namespace HotelBooking.Domain.Exceptions;

public sealed class BookingConflictException : DomainException
{
    public BookingConflictException(string message) : base(message, 409) { }
}