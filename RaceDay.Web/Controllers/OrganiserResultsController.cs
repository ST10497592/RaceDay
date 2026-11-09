using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// =======================================================
// START: ORGANISER RESULTS CONTROLLER
// Purpose: Record, edit and delete race results.
// Finish times are typed as h:mm:ss and parsed here.
// =======================================================

[SessionAuthorize("Organiser")]
[Route("organiser/results/{action=Index}/{id?}")]
public class OrganiserResultsController : AppController
{
    private static readonly string[] TimeFormats = { @"h\:mm\:ss", @"hh\:mm\:ss" };

    public OrganiserResultsController(RaceDayApiClient api, UserSession currentUser)
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

        return View(result.Data.OrderByDescending(e => e.EventDate).ToList());
    }

    // Step 2: results for one event, plus enrolments still waiting for a result.
    [HttpGet]
    public async Task<IActionResult> Manage(int id)
    {
        var loaded = await LoadEvent(id);

        if (loaded.Redirect != null)
            return loaded.Redirect;

        var results = await Api.GetEventResultsAsync(id);
        var enrolments = await Api.GetEventEnrolmentsAsync(id);

        if (!results.Success || results.Data == null)
            return FailWith(results, RedirectToAction(nameof(Index)));

        if (!enrolments.Success || enrolments.Data == null)
            return FailWith(enrolments, RedirectToAction(nameof(Index)));

        return View(new EventResultsViewModel
        {
            Event = loaded.Event!,
            Results = results.Data
                .OrderBy(r => r.Position ?? int.MaxValue)
                .ThenBy(r => r.ParticipantName)
                .ToList(),
            WithoutResult = enrolments.Data
                .Where(e => e.ResultId == null && e.Status != "Cancelled")
                .OrderBy(e => e.ParticipantName)
                .ToList()
        });
    }

    // START: ADD
    [HttpGet]
    public async Task<IActionResult> Add(int id, int? enrolmentId = null)
    {
        var loaded = await LoadEvent(id);

        if (loaded.Redirect != null)
            return loaded.Redirect;

        var model = await BuildForm(id, loaded.Event!.EventName, null);

        if (model == null)
            return RedirectToAction(nameof(Manage), new { id });

        model.EnrolmentId = enrolmentId;

        return View("Form", model);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int id, ResultFormViewModel model)
    {
        model.EventId = id;

        var loaded = await LoadEvent(id);

        if (loaded.Redirect != null)
            return loaded.Redirect;

        model.EventName = loaded.Event!.EventName;

        var parsed = ValidateForm(model, out var finishTime);

        if (!ModelState.IsValid)
            return await RedisplayForm(model, null);

        var result = await Api.CreateResultAsync(
            model.EnrolmentId!.Value,
            model.ResultStatus,
            finishTime,
            parsed ? model.Position : null);

        if (!result.Success)
        {
            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            ModelState.AddModelError(string.Empty, result.Error ?? "We could not save the result.");

            return await RedisplayForm(model, null);
        }

        Success("Result recorded.");

        return RedirectToAction(nameof(Manage), new { id });
    }
    // END: ADD

    // START: EDIT (id = result id, eventId = its event)
    [HttpGet]
    public async Task<IActionResult> Edit(int id, int eventId)
    {
        var loaded = await LoadEvent(eventId);

        if (loaded.Redirect != null)
            return loaded.Redirect;

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);

        if (!enrolments.Success || enrolments.Data == null)
            return FailWith(enrolments, RedirectToAction(nameof(Manage), new { id = eventId }));

        var current = enrolments.Data.FirstOrDefault(e => e.ResultId == id);

        if (current == null)
        {
            Fail("That result could not be found.");

            return RedirectToAction(nameof(Manage), new { id = eventId });
        }

        var model = new ResultFormViewModel
        {
            EventId = eventId,
            EventName = loaded.Event!.EventName,
            ResultId = id,
            EnrolmentId = current.EnrolmentId,
            ResultStatus = current.ResultStatus ?? "Finished",
            FinishTime = current.FinishTime.HasValue
                ? current.FinishTime.Value.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
                : null,
            Position = current.Position,
            Candidates = new List<EnrolmentDto> { current }
        };

        return View("Form", model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, int eventId, ResultFormViewModel model)
    {
        model.ResultId = id;
        model.EventId = eventId;

        var loaded = await LoadEvent(eventId);

        if (loaded.Redirect != null)
            return loaded.Redirect;

        model.EventName = loaded.Event!.EventName;

        var parsed = ValidateForm(model, out var finishTime);

        if (!ModelState.IsValid)
            return await RedisplayForm(model, id);

        var result = await Api.UpdateResultAsync(
            id,
            model.EnrolmentId!.Value,
            model.ResultStatus,
            finishTime,
            parsed ? model.Position : null);

        if (!result.Success)
        {
            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            ModelState.AddModelError(string.Empty, result.Error ?? "We could not update the result.");

            return await RedisplayForm(model, id);
        }

        Success("Result updated.");

        return RedirectToAction(nameof(Manage), new { id = eventId });
    }
    // END: EDIT

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await Api.DeleteResultAsync(id);

        if (!result.Success)
            return FailWith(result, RedirectToAction(nameof(Manage), new { id = eventId }));

        Success("Result deleted.");

        return RedirectToAction(nameof(Manage), new { id = eventId });
    }

    // START: HELPERS
    private sealed class LoadedEvent
    {
        public EventDto? Event { get; set; }
        public IActionResult? Redirect { get; set; }
    }

    private async Task<LoadedEvent> LoadEvent(int id)
    {
        var result = await Api.GetEventAsync(id);

        if (!result.Success || result.Data == null)
            return new LoadedEvent { Redirect = FailWith(result, RedirectToAction(nameof(Index))) };

        if (CurrentUser.UserId.HasValue && result.Data.OrganiserId != CurrentUser.UserId.Value)
        {
            Fail("You can only record results for events that you created.");

            return new LoadedEvent { Redirect = RedirectToAction(nameof(Index)) };
        }

        return new LoadedEvent { Event = result.Data };
    }

    // Enrolments that can still receive a result.
    private async Task<ResultFormViewModel?> BuildForm(int eventId, string eventName, int? resultId)
    {
        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);

        if (!enrolments.Success || enrolments.Data == null)
        {
            var expired = HandleExpiredSession(enrolments);

            if (expired == null)
                Fail(enrolments.Error ?? "We could not load the participants.");

            return null;
        }

        var candidates = enrolments.Data
            .Where(e => e.ResultId == null && e.Status != "Cancelled")
            .OrderBy(e => e.ParticipantName)
            .ToList();

        if (candidates.Count == 0 && resultId == null)
        {
            Fail("Every enrolled participant already has a result, or nobody has enrolled yet.");

            return null;
        }

        return new ResultFormViewModel
        {
            EventId = eventId,
            EventName = eventName,
            Candidates = candidates
        };
    }

    private async Task<IActionResult> RedisplayForm(ResultFormViewModel model, int? resultId)
    {
        if (resultId.HasValue)
        {
            var enrolments = await Api.GetEventEnrolmentsAsync(model.EventId);

            if (enrolments.Success && enrolments.Data != null)
            {
                model.Candidates = enrolments.Data
                    .Where(e => e.ResultId == resultId.Value)
                    .ToList();
            }
        }
        else
        {
            var rebuilt = await BuildForm(model.EventId, model.EventName, null);

            if (rebuilt != null)
                model.Candidates = rebuilt.Candidates;
        }

        return View("Form", model);
    }

    // Returns true when the finish time and position apply (status is Finished).
    private bool ValidateForm(ResultFormViewModel model, out TimeSpan? finishTime)
    {
        finishTime = null;

        if (model.ResultStatus != "Finished")
        {
            // Finish time and position do not apply to non-finishers.
            ModelState.Remove(nameof(model.FinishTime));
            ModelState.Remove(nameof(model.Position));

            return false;
        }

        if (string.IsNullOrWhiteSpace(model.FinishTime))
        {
            ModelState.AddModelError(nameof(model.FinishTime), "Please enter the finish time, for example 1:45:30.");
        }
        else if (TimeSpan.TryParseExact(model.FinishTime.Trim(), TimeFormats, CultureInfo.InvariantCulture, out var parsed))
        {
            if (parsed <= TimeSpan.Zero)
                ModelState.AddModelError(nameof(model.FinishTime), "The finish time must be greater than zero.");
            else
                finishTime = parsed;
        }
        else
        {
            ModelState.AddModelError(nameof(model.FinishTime), "Finish time must look like 1:45:30 (hours:minutes:seconds).");
        }

        if (!model.Position.HasValue)
        {
            ModelState.AddModelError(nameof(model.Position), "Please enter the finishing position.");
        }

        return true;
    }
    // END: HELPERS
}

// =======================================================
// END: ORGANISER RESULTS CONTROLLER
// =======================================================
