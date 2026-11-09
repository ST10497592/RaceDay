using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// =======================================================
// START: ORGANISER ENROLMENTS CONTROLLER
// Purpose: Review the enrolments for the organiser's events and
// confirm, cancel or update payment status.
// =======================================================

[SessionAuthorize("Organiser")]
[Route("organiser/enrolments/{action=Index}/{id?}")]
public class OrganiserEnrolmentsController : AppController
{
    private static readonly string[] Statuses = { "Pending", "Confirmed", "Cancelled", "Completed" };
    private static readonly string[] PaymentStatuses = { "Pending", "Paid", "Failed", "Refunded" };

    public OrganiserEnrolmentsController(RaceDayApiClient api, UserSession currentUser)
        : base(api, currentUser)
    {
    }

    // Step 1: pick one of my events.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyEventsAsync();

        if (!result.Success || result.Data == null)
        {
            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            Fail(result.Error ?? "We could not load your events.");

            return View(new List<EventSummaryDto>());
        }

        return View(result.Data.OrderBy(e => e.EventDate).ToList());
    }

    // Step 2: enrolments for one event.
    [HttpGet]
    public async Task<IActionResult> Manage(int id, string? status = null)
    {
        var eventResult = await Api.GetEventAsync(id);

        if (!eventResult.Success || eventResult.Data == null)
            return FailWith(eventResult, RedirectToAction(nameof(Index)));

        if (CurrentUser.UserId.HasValue && eventResult.Data.OrganiserId != CurrentUser.UserId.Value)
        {
            Fail("You can only manage enrolments for events that you created.");

            return RedirectToAction(nameof(Index));
        }

        var enrolments = await Api.GetEventEnrolmentsAsync(id);

        if (!enrolments.Success || enrolments.Data == null)
            return FailWith(enrolments, RedirectToAction(nameof(Index)));

        var list = enrolments.Data;

        if (!string.IsNullOrWhiteSpace(status))
            list = list.Where(e => e.Status == status).ToList();

        return View(new EventEnrolmentsViewModel
        {
            Event = eventResult.Data,
            Enrolments = list.OrderBy(e => e.EnrolmentDate).ToList(),
            StatusFilter = status ?? string.Empty
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, int eventId, string status, string paymentStatus)
    {
        if (!Statuses.Contains(status) || !PaymentStatuses.Contains(paymentStatus))
        {
            Fail("Please choose a valid enrolment and payment status.");

            return RedirectToAction(nameof(Manage), new { id = eventId });
        }

        var result = await Api.UpdateEnrolmentStatusAsync(id, status, paymentStatus);

        if (!result.Success)
            return FailWith(result, RedirectToAction(nameof(Manage), new { id = eventId }));

        Success("Enrolment updated to " + status + " (payment: " + paymentStatus + ").");

        return RedirectToAction(nameof(Manage), new { id = eventId });
    }
}

// =======================================================
// END: ORGANISER ENROLMENTS CONTROLLER
// =======================================================
