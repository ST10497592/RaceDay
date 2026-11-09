using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// =======================================================
// START: PARTICIPANT CONTROLLER
// Purpose: A participant's own enrolments and results.
// =======================================================

[SessionAuthorize("Participant")]
[Route("my/{action}")]
public class ParticipantController : AppController
{
    public ParticipantController(RaceDayApiClient api, UserSession currentUser)
        : base(api, currentUser)
    {
    }

    // START: MY ENROLMENTS
    [HttpGet]
    public async Task<IActionResult> MyEnrolments()
    {
        var result = await Api.GetMyEnrolmentsAsync();

        if (!result.Success || result.Data == null)
        {
            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            Fail(result.Error ?? "We could not load your enrolments.");

            return View(new MyEnrolmentsViewModel());
        }

        return View(new MyEnrolmentsViewModel
        {
            Enrolments = result.Data.OrderByDescending(e => e.EventDate).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var result = await Api.CancelEnrolmentAsync(id);

        if (!result.Success)
            return FailWith(result, RedirectToAction(nameof(MyEnrolments)));

        Success("Your enrolment has been cancelled.");

        return RedirectToAction(nameof(MyEnrolments));
    }
    // END: MY ENROLMENTS

    // START: MY RESULTS
    [HttpGet]
    public async Task<IActionResult> MyResults()
    {
        var result = await Api.GetMyResultsAsync();

        if (!result.Success || result.Data == null)
        {
            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            Fail(result.Error ?? "We could not load your results.");

            return View(new MyResultsViewModel());
        }

        return View(new MyResultsViewModel { Results = result.Data });
    }
    // END: MY RESULTS
}

// =======================================================
// END: PARTICIPANT CONTROLLER
// =======================================================
