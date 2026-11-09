using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// =======================================================
// START: EVENTS CONTROLLER (PUBLIC BROWSING + ENROLMENT)
// Purpose: Anyone can browse events. Participants can enrol.
// =======================================================

public class EventsController : AppController
{
    public EventsController(RaceDayApiClient api, UserSession currentUser)
        : base(api, currentUser)
    {
    }

    // START: BROWSE EVENTS
    [HttpGet]
    public async Task<IActionResult> Index(EventQuery query)
    {
        var model = new EventListViewModel { Query = query };

        var result = await Api.GetEventsAsync(query);

        if (result.Success && result.Data != null)
        {
            model.Events = result.Data
                .OrderBy(e => e.EventDate)
                .ThenBy(e => e.StartTime)
                .ToList();
        }
        else
        {
            model.Error = result.Error;
        }

        return View(model);
    }
    // END: BROWSE EVENTS

    // START: EVENT DETAILS
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var eventResult = await Api.GetEventAsync(id);

        if (!eventResult.Success || eventResult.Data == null)
        {
            if (eventResult.IsNotFound)
                return NotFound();

            Fail(eventResult.Error ?? "We could not load that event.");

            return RedirectToAction(nameof(Index));
        }

        var model = new EventDetailsViewModel
        {
            Event = eventResult.Data,
            IsSignedIn = CurrentUser.IsAuthenticated,
            IsParticipant = CurrentUser.IsParticipant
        };

        var results = await Api.GetEventResultsAsync(id);

        if (results.Success && results.Data != null)
        {
            model.Results = results.Data
                .Where(r => r.ResultStatus == "Finished")
                .OrderBy(r => r.Position ?? int.MaxValue)
                .ToList();
        }

        if (CurrentUser.IsParticipant)
        {
            var mine = await Api.GetMyEnrolmentsAsync();

            if (mine.Success && mine.Data != null)
            {
                model.MyEnrolments = mine.Data.Where(e => e.EventId == id).ToList();
                model.AlreadyEnrolled = model.MyEnrolments.Any(e => e.Status != "Cancelled");
            }
            else
            {
                var expired = HandleExpiredSession(mine);

                if (expired != null)
                    return expired;
            }
        }

        return View(model);
    }
    // END: EVENT DETAILS

    // START: ENROL
    [HttpPost]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> Enrol(int id, int? categoryId)
    {
        if (!categoryId.HasValue)
        {
            Fail("Please choose a category before enrolling.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await Api.EnrolAsync(id, categoryId.Value);

        if (!result.Success)
            return FailWith(result, RedirectToAction(nameof(Details), new { id }));

        Success("You are enrolled! Your enrolment is pending until the organiser confirms it.");

        return RedirectToAction("MyEnrolments", "Participant");
    }
    // END: ENROL
}

// =======================================================
// END: EVENTS CONTROLLER
// =======================================================
