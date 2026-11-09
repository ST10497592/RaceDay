using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// =======================================================
// START: HOME CONTROLLER
// Purpose: Landing page and the friendly error page.
// =======================================================

public class HomeController : AppController
{
    public HomeController(RaceDayApiClient api, UserSession currentUser)
        : base(api, currentUser)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Organisers work from their dashboard.
        if (CurrentUser.IsOrganiser)
            return RedirectToAction("Dashboard", "Organiser");

        var model = new HomeViewModel();
        var result = await Api.GetEventsAsync(new EventQuery { Upcoming = true });

        if (result.Success && result.Data != null)
        {
            model.FeaturedEvents = result.Data
                .Where(e => e.Status == "Open" || e.Status == "Upcoming")
                .OrderBy(e => e.EventDate)
                .Take(3)
                .ToList();
        }
        else
        {
            model.Error = result.Error;
        }

        return View(model);
    }

    // Reached for unhandled exceptions and for 404/403/... status codes.
    [IgnoreAntiforgeryToken]
    public IActionResult Error(int? statusCode = null)
    {
        var code = statusCode ?? Response.StatusCode;

        if (code < 400)
            code = 500;

        var model = new ErrorViewModel { StatusCode = code };

        switch (code)
        {
            case 404:
                model.Title = "Page not found";
                model.Message = "We could not find the page you were looking for. It may have been moved or no longer exists.";
                break;
            case 403:
                model.Title = "Access denied";
                model.Message = "You do not have permission to view this page.";
                break;
            case 400:
                model.Title = "Something was not right";
                model.Message = "We could not understand that request. Please go back and try again.";
                break;
            default:
                model.Title = "Something went wrong";
                model.Message = "An unexpected problem occurred on our side. Please try again in a moment.";
                break;
        }

        Response.StatusCode = code;

        return View(model);
    }
}

public class ErrorViewModel
{
    public int StatusCode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

// =======================================================
// END: HOME CONTROLLER
// =======================================================
