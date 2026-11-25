using HotelBooking.BLL.Interfaces;
using HotelBooking.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers;

public sealed class HotelsController : Controller
{
    private readonly IHotelService _hotelService;

    public HotelsController(IHotelService hotelService)
    {
        _hotelService = hotelService;
    }

    public async Task<IActionResult> Index(string? city)
    {
        ViewBag.Cities = await _hotelService.GetActiveCitiesAsync();
        ViewBag.SelectedCity = city;
        var rooms = await _hotelService.GetRoomCatalogAsync(city: city, onlyActiveHotels: true);
        return View(rooms);
    }

    public async Task<IActionResult> Details(int id)
    {
        var hotel = await _hotelService.GetHotelByIdAsync(id);
        var rooms = await _hotelService.GetRoomCatalogAsync(hotelId: id, onlyActiveHotels: false);

        return View(new HotelDetailsViewModel
        {
            Hotel = hotel,
            Rooms = rooms
        });
    }

    public async Task<IActionResult> Room(int id, DateOnly? checkIn, DateOnly? checkOut, short? guests)
    {
        var room = await _hotelService.GetRoomDetailsAsync(id);
        var from = checkIn ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var to = checkOut ?? DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        if (to <= from) to = from.AddDays(1);

        var nights = to.DayNumber - from.DayNumber;
        var model = new RoomDetailsViewModel
        {
            Room = room,
            CheckIn = from,
            CheckOut = to,
            GuestsCount = guests ?? 1,
            EstimatedPrice = room.BasePrice * nights
        };

        return View(model);
    }

    [HttpGet("/api/rooms/search")]
    public async Task<IActionResult> ApiSearch(string? city, DateOnly checkIn, DateOnly checkOut, short guests = 1)
    {
        var results = await _hotelService.SearchAvailableRoomsAsync(city, checkIn, checkOut, guests);
        return Ok(results);
    }

    [HttpGet("/api/rooms/{roomId:int}/price")]
    public async Task<IActionResult> ApiCalculatePrice(int roomId, DateOnly checkIn, DateOnly checkOut)
    {
        var total = await _hotelService.CalculateStayPriceAsync(roomId, checkIn, checkOut);
        var nights = checkOut.DayNumber - checkIn.DayNumber;
        return Ok(new { roomId, nights, totalPrice = total });
    }
}