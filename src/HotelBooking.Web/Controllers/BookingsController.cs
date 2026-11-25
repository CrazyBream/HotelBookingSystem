using HotelBooking.BLL.DTOs.Booking;
using HotelBooking.BLL.Interfaces;
using HotelBooking.Domain.Exceptions;
using HotelBooking.Web.Extensions;
using HotelBooking.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers;

[Authorize]
public sealed class BookingsController : Controller
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    [Authorize(Roles = "guest")]
    public async Task<IActionResult> Index()
    {
        var bookings = await _bookingService.GetGuestBookingsAsync(User.GetUserId());
        return View(bookings);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (!User.IsStaffUser() && booking.GuestId != User.GetUserId())
            return Forbid();

        var payments = await _bookingService.GetPaymentsAsync(id);
        return View(new BookingDetailsViewModel
        {
            Booking = booking,
            Payments = payments
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "guest")]
    public async Task<IActionResult> Create(CreateBookingDto dto)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Перевірте параметри бронювання.";
            return RedirectToAction("Room", "Hotels", new { id = dto.RoomId });
        }

        try
        {
            var result = await _bookingService.CreateBookingAsync(User.GetUserId(), dto);
            TempData["Success"] = $"Бронювання {result.BookingCode} створено на суму {result.TotalPrice:N2} грн.";
            return RedirectToAction(nameof(Details), new { id = result.BookingId });
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Room", "Hotels", new
            {
                id = dto.RoomId,
                checkIn = dto.CheckInDate.ToString("yyyy-MM-dd"),
                checkOut = dto.CheckOutDate.ToString("yyyy-MM-dd"),
                guests = dto.GuestsCount
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "guest")]
    public async Task<IActionResult> Cancel(long id)
    {
        try
        {
            await _bookingService.CancelBookingAsync(id, User.GetUserId());
            TempData["Success"] = "Бронювання скасовано.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(CreatePaymentDto dto)
    {
        var booking = await _bookingService.GetBookingByIdAsync(dto.BookingId);
        if (!User.IsStaffUser() && booking.GuestId != User.GetUserId())
            return Forbid();

        try
        {
            int? staffId = User.IsStaffUser() ? User.GetUserId() : null;
            await _bookingService.AddPaymentAsync(dto, staffId);
            TempData["Success"] = $"Оплату на суму {dto.Amount:N2} грн успішно проведено.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = dto.BookingId });
    }
}