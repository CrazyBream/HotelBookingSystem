using HotelBooking.BLL.DTOs.Booking;
using HotelBooking.BLL.DTOs.Hotel;
using HotelBooking.BLL.Interfaces;
using HotelBooking.Domain.Exceptions;
using HotelBooking.Web.Extensions;
using HotelBooking.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers;

[Authorize(Roles = "admin,manager,receptionist,analyst")]
public sealed class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IBookingService _bookingService;
    private readonly IHotelService _hotelService;

    public AdminController(
        IAdminService adminService,
        IBookingService bookingService,
        IHotelService hotelService)
    {
        _adminService = adminService;
        _bookingService = bookingService;
        _hotelService = hotelService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var dashboard = await _adminService.GetDashboardAsync();
        return View(dashboard);
    }

    [HttpGet]
    public async Task<IActionResult> Bookings(string? status, int? hotelId)
    {
        var model = new AdminBookingsViewModel
        {
            Bookings = await _bookingService.GetAllBookingsAsync(status, hotelId),
            Hotels = await _hotelService.GetAllHotelsAsync(onlyActive: false),
            SelectedStatus = status,
            SelectedHotelId = hotelId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,manager,receptionist")]
    public async Task<IActionResult> ConfirmBooking(long id)
    {
        await RunActionAsync(() => _bookingService.ConfirmBookingAsync(id), "Бронювання підтверджено.");
        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,manager,receptionist")]
    public async Task<IActionResult> CancelBooking(long id)
    {
        await RunActionAsync(() => _bookingService.CancelBookingAsync(id), "Бронювання скасовано.");
        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,manager,receptionist")]
    public async Task<IActionResult> MarkNoShow(long id)
    {
        await RunActionAsync(() => _bookingService.MarkNoShowAsync(id), "Позначено як неявку (no_show).");
        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,manager,receptionist")]
    public async Task<IActionResult> CheckIn(CheckInDto dto)
    {
        await RunActionAsync(
            () => _bookingService.CheckInGuestAsync(dto, User.GetUserId()),
            "Гостя успішно заселено (sp_check_in_guest).");
        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,manager,receptionist")]
    public async Task<IActionResult> CheckOut(long bookingId, string? notes)
    {
        await RunActionAsync(
            () => _bookingService.CheckOutGuestAsync(bookingId, User.GetUserId(), notes),
            "Гостя успішно виселено (sp_check_out_guest).");
        return RedirectToAction(nameof(Bookings));
    }

    [HttpGet]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> Hotels()
    {
        var model = new AdminHotelsViewModel
        {
            Hotels = await _hotelService.GetAllHotelsAsync(onlyActive: false),
            Rooms = await _hotelService.GetRoomCatalogAsync(onlyActiveHotels: false),
            Categories = await _hotelService.GetCategoriesAsync(),
            Amenities = await _hotelService.GetAmenitiesAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> CreateHotel([Bind(Prefix = "NewHotel")] CreateHotelDto dto)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Заповніть усі обов’язкові поля готелю.";
            return RedirectToAction(nameof(Hotels));
        }

        await RunActionAsync(() => _hotelService.CreateHotelAsync(dto), $"Готель «{dto.Name}» додано.");
        return RedirectToAction(nameof(Hotels));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> ToggleHotelActive(int id, bool isActive)
    {
        await RunActionAsync(
            () => _hotelService.SetHotelActiveAsync(id, isActive),
            isActive ? "Готель активовано." : "Готель деактивовано.");
        return RedirectToAction(nameof(Hotels));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> CreateRoom([Bind(Prefix = "NewRoom")] CreateRoomDto dto)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Заповніть усі обов’язкові поля номера.";
            return RedirectToAction(nameof(Hotels));
        }

        await RunActionAsync(() => _hotelService.CreateRoomAsync(dto), $"Номер {dto.RoomNumber} додано.");
        return RedirectToAction(nameof(Hotels));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,manager,receptionist")]
    public async Task<IActionResult> UpdateRoomStatus(int roomId, string status)
    {
        await RunActionAsync(
            () => _hotelService.UpdateRoomStatusAsync(roomId, status),
            $"Статус номера змінено на {status}.");
        return RedirectToAction(nameof(Hotels));
    }

    [HttpGet]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> Guests()
    {
        var guests = await _adminService.GetAllGuestsAsync();
        return View(guests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> ToggleGuestBan(int id, bool isBanned)
    {
        await RunActionAsync(
            () => _adminService.SetGuestBannedAsync(id, isBanned),
            isBanned ? "Гостя заблоковано." : "Гостя розблоковано.");
        return RedirectToAction(nameof(Guests));
    }

    private async Task RunActionAsync(Func<Task> action, string successMessage)
    {
        try
        {
            await action();
            TempData["Success"] = successMessage;
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
    }
}