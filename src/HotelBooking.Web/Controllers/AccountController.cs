using System.Security.Claims;
using HotelBooking.BLL.DTOs.Auth;
using HotelBooking.BLL.Interfaces;
using HotelBooking.Domain.Exceptions;
using HotelBooking.Web.Extensions;
using HotelBooking.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IBookingService _bookingService;

    public AccountController(IAuthService authService, IBookingService bookingService)
    {
        _authService = authService;
        _bookingService = bookingService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            var user = await _authService.LoginAsync(dto);
            await SignInAsync(user);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return user.IsStaff
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Profile", "Account");
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterDto());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            var user = await _authService.RegisterGuestAsync(dto);
            await SignInAsync(user);
            TempData["Success"] = "Реєстрацію успішно завершено.";
            return RedirectToAction("Profile");
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize(Roles = "guest")]
    public async Task<IActionResult> Profile()
    {
        var guestId = User.GetUserId();
        var guest = await _authService.GetGuestProfileAsync(guestId);
        var bookings = await _bookingService.GetGuestBookingsAsync(guestId);

        var model = new ProfileViewModel
        {
            Email = guest.Email,
            Profile = new UpdateProfileDto
            {
                FirstName = guest.FirstName,
                LastName = guest.LastName,
                Phone = guest.Phone,
                DocumentType = guest.DocumentType,
                DocumentNumber = guest.DocumentNumber
            },
            Bookings = bookings
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "guest")]
    public async Task<IActionResult> UpdateProfile([Bind(Prefix = "Profile")] UpdateProfileDto dto)
    {
        var guestId = User.GetUserId();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Перевірте коректність заповнених полів профілю.";
            return RedirectToAction(nameof(Profile));
        }

        try
        {
            await _authService.UpdateGuestProfileAsync(guestId, dto);
            TempData["Success"] = "Профіль успішно оновлено.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(AuthenticatedUserDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("IsStaff", user.IsStaff ? "true" : "false")
        };

        if (user.HotelId.HasValue)
            claims.Add(new Claim("HotelId", user.HotelId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });
    }
}