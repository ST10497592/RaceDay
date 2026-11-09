using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// =======================================================
// START: ORGANISER DASHBOARD CONTROLLER
// Purpose: Overview of the organiser's events and enrolments.
// =======================================================

[SessionAuthorize("Organiser")]
[Route("organiser/{action=Dashboard}")]
public class OrganiserController : AppController
{
    public OrganiserController(RaceDayApiClient api, UserSession currentUser)
        : base(api, currentUser)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var model = new OrganiserDashboardViewModel();
        var result = await Api.GetMyEventsAsync();

        if (result.Success && result.Data != null)
        {
            model.Events = result.Data.OrderBy(e => e.EventDate).ToList();
        }
        else
        {
            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            model.Error = result.Error;
        }

        return View(model);
    }
}

// =======================================================
// END: ORGANISER DASHBOARD CONTROLLER
// =======================================================
