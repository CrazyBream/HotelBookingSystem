using System.Security.Claims;

namespace HotelBooking.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id)
            ? id
            : throw new InvalidOperationException("Ідентифікатор користувача відсутній.");
    }

    public static string GetUserRole(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public static bool IsStaffUser(this ClaimsPrincipal user) =>
        user.FindFirstValue("IsStaff") == "true";
}