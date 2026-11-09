using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// =======================================================
// START: APP CONTROLLER (BASE CLASS)
// Purpose: Shared helpers for every controller - flash messages,
// session-expiry handling and client-side image checks.
// =======================================================

public abstract class AppController : Controller
{
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxImageBytes = 5 * 1024 * 1024;

    protected readonly RaceDayApiClient Api;
    protected readonly UserSession CurrentUser;

    protected AppController(RaceDayApiClient api, UserSession currentUser)
    {
        Api = api;
        CurrentUser = currentUser;
    }

    protected void Success(string message)
    {
        TempData["Success"] = message;
    }

    protected void Fail(string message)
    {
        TempData["Error"] = message;
    }

    protected void Warn(string message)
    {
        TempData["Warning"] = message;
    }

    // Sends the person to the right home page for their role.
    protected IActionResult RedirectToRoleHome()
    {
        if (CurrentUser.IsOrganiser)
            return RedirectToAction("Dashboard", "Organiser");

        return RedirectToAction("Index", "Home");
    }

    // Returns a redirect when the API rejected the token (expired session), otherwise null.
    protected IActionResult? HandleExpiredSession(ApiResult result)
    {
        if (!result.IsUnauthorized)
            return null;

        CurrentUser.Clear();
        Fail("Your session has expired. Please sign in again.");

        return RedirectToAction("Login", "Account");
    }

    // Flashes the API error and returns the fallback result - or the login redirect when the session expired.
    protected IActionResult FailWith(ApiResult result, IActionResult fallback)
    {
        var expired = HandleExpiredSession(result);

        if (expired != null)
            return expired;

        Fail(result.Error ?? "Something went wrong. Please try again.");

        return fallback;
    }

    // Checks an uploaded image before it is sent to the API. Returns an error message or null.
    protected static string? ValidateImage(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return "Please choose an image to upload.";

        if (file.Length > MaxImageBytes)
            return "The image is too large. The maximum size is 5 MB.";

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!ImageExtensions.Contains(extension))
            return "Only JPG, PNG, GIF or WEBP images are allowed.";

        return null;
    }
}

// =======================================================
// END: APP CONTROLLER (BASE CLASS)
// =======================================================
