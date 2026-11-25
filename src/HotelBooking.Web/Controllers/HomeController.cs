using System.Diagnostics;
using HotelBooking.BLL.Interfaces;
using HotelBooking.Domain.Exceptions;
using HotelBooking.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers;

public sealed class HomeController : Controller
{
    private readonly IHotelService _hotelService;

    public HomeController(IHotelService hotelService)
    {
        _hotelService = hotelService;
    }

    public async Task<IActionResult> Index(string? city, DateOnly? checkIn, DateOnly? checkOut, short? guests)
    {
        var model = new HomeIndexViewModel
        {
            Hotels = await _hotelService.GetAllHotelsAsync(onlyActive: true),
            Cities = await _hotelService.GetActiveCitiesAsync(),
            SelectedCity = city,
            CheckIn = checkIn ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            CheckOut = checkOut ?? DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            Guests = guests ?? 2
        };

        if (checkIn.HasValue && checkOut.HasValue)
        {
            try
            {
                model.SearchResults = await _hotelService.SearchAvailableRoomsAsync(
                    city, model.CheckIn, model.CheckOut, model.Guests);
            }
            catch (DomainException ex)
            {
                TempData["Error"] = ex.Message;
            }
        }

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(string? message)
    {
        ViewBag.ErrorMessage = message;
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}