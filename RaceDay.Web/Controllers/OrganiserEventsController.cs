using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// =======================================================
// START: ORGANISER EVENTS CONTROLLER
// Purpose: Create, edit, delete events, upload banners and
// assign categories. Ownership is enforced by the API; the MVC
// app also checks it so the organiser gets a clear message.
// =======================================================

[SessionAuthorize("Organiser")]
[Route("organiser/events/{action=Index}/{id?}")]
public class OrganiserEventsController : AppController
{
    public OrganiserEventsController(RaceDayApiClient api, UserSession currentUser)
        : base(api, currentUser)
    {
    }

    // START: LIST
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
    // END: LIST

    // START: CREATE
    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new EventFormViewModel
        {
            EventDate = DateTime.Today.AddDays(30)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        ValidateForm(model);

        if (!ModelState.IsValid)
            return View("Form", model);

        var result = await Api.CreateEventAsync(ToFormData(model), null);

        if (!result.Success || result.Data == null)
        {
            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            ModelState.AddModelError(string.Empty, result.Error ?? "We could not create the event.");

            return View("Form", model);
        }

        var eventId = result.Data.EventId;

        await UploadBannerIfProvided(eventId, model.Banner);

        Success("Event created. Now choose which categories runners can enter.");

        return RedirectToAction(nameof(Categories), new { id = eventId });
    }
    // END: CREATE

    // START: EDIT
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var existing = await LoadOwnedEvent(id);

        if (existing.Redirect != null)
            return existing.Redirect;

        return View("Form", ToViewModel(existing.Event!));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;

        // The current banner is re-read from the API instead of trusting the posted form.
        var existing = await LoadOwnedEvent(id);

        if (existing.Redirect != null)
            return existing.Redirect;

        model.BannerUrl = existing.Event!.BannerUrl;

        ValidateForm(model);

        if (!ModelState.IsValid)
            return View("Form", model);

        var result = await Api.UpdateEventAsync(id, ToFormData(model), existing.Event.BannerUrl);

        if (!result.Success)
        {
            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            ModelState.AddModelError(string.Empty, result.Error ?? "We could not update the event.");

            return View("Form", model);
        }

        await UploadBannerIfProvided(id, model.Banner);

        Success("Event updated.");

        return RedirectToAction(nameof(Index));
    }
    // END: EDIT

    // START: DELETE
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteEventAsync(id);

        if (!result.Success)
        {
            if (result.IsConflict)
            {
                Fail("This event already has enrolments, so it cannot be deleted. Set its status to Cancelled instead.");

                return RedirectToAction(nameof(Index));
            }

            return FailWith(result, RedirectToAction(nameof(Index)));
        }

        Success("Event deleted.");

        return RedirectToAction(nameof(Index));
    }
    // END: DELETE

    // START: CATEGORIES FOR AN EVENT
    [HttpGet]
    public async Task<IActionResult> Categories(int id)
    {
        var existing = await LoadOwnedEvent(id);

        if (existing.Redirect != null)
            return existing.Redirect;

        var categories = await Api.GetCategoriesAsync();

        if (!categories.Success || categories.Data == null)
            return FailWith(categories, RedirectToAction(nameof(Index)));

        var model = new ManageEventCategoriesViewModel
        {
            Event = existing.Event!,
            AllCategories = categories.Data.OrderBy(c => c.CategoryName).ToList(),
            AssignedIds = existing.Event!.Categories.Select(c => c.CategoryId).ToHashSet()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(int id, int categoryId)
    {
        var result = await Api.AddCategoryToEventAsync(id, categoryId);

        if (!result.Success)
            return FailWith(result, RedirectToAction(nameof(Categories), new { id }));

        Success("Category added to the event.");

        return RedirectToAction(nameof(Categories), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveCategory(int id, int categoryId)
    {
        var result = await Api.RemoveCategoryFromEventAsync(id, categoryId);

        if (!result.Success)
            return FailWith(result, RedirectToAction(nameof(Categories), new { id }));

        Success("Category removed from the event.");

        return RedirectToAction(nameof(Categories), new { id });
    }
    // END: CATEGORIES FOR AN EVENT

    // START: HELPERS
    private sealed class OwnedEventResult
    {
        public EventDto? Event { get; set; }
        public IActionResult? Redirect { get; set; }
    }

    private async Task<OwnedEventResult> LoadOwnedEvent(int id)
    {
        var result = await Api.GetEventAsync(id);

        if (!result.Success || result.Data == null)
        {
            if (result.IsNotFound)
            {
                Fail("That event could not be found.");

                return new OwnedEventResult { Redirect = RedirectToAction(nameof(Index)) };
            }

            return new OwnedEventResult { Redirect = FailWith(result, RedirectToAction(nameof(Index))) };
        }

        if (CurrentUser.UserId.HasValue && result.Data.OrganiserId != CurrentUser.UserId.Value)
        {
            Fail("You can only manage events that you created.");

            return new OwnedEventResult { Redirect = RedirectToAction(nameof(Index)) };
        }

        return new OwnedEventResult { Event = result.Data };
    }

    private async Task UploadBannerIfProvided(int eventId, IFormFile? banner)
    {
        if (banner == null || banner.Length == 0)
            return;

        var problem = ValidateImage(banner);

        if (problem != null)
        {
            Warn("The event was saved, but the banner was not uploaded: " + problem);
            return;
        }

        var upload = await Api.UploadEventBannerAsync(eventId, banner);

        if (!upload.Success)
        {
            Warn("The event was saved, but the banner could not be uploaded: " + (upload.Error ?? "please try again."));
        }
    }

    private void ValidateForm(EventFormViewModel model)
    {
        if (!TimeSpan.TryParseExact(model.StartTime ?? string.Empty, @"hh\:mm", CultureInfo.InvariantCulture, out _))
        {
            ModelState.AddModelError(nameof(model.StartTime), "Start time must look like 06:30.");
        }

        if (model.Banner != null && model.Banner.Length > 0)
        {
            var problem = ValidateImage(model.Banner);

            if (problem != null)
                ModelState.AddModelError(nameof(model.Banner), problem);
        }
    }

    private static EventFormData ToFormData(EventFormViewModel model)
    {
        TimeSpan.TryParseExact(model.StartTime, @"hh\:mm", CultureInfo.InvariantCulture, out var start);

        return new EventFormData
        {
            EventName = model.EventName.Trim(),
            Description = model.Description.Trim(),
            EventType = model.EventType,
            EventDate = model.EventDate ?? DateTime.Today,
            StartTime = start,
            Location = model.Location.Trim(),
            EntryFee = model.EntryFee,
            MaxParticipants = model.MaxParticipants,
            Status = model.Status
        };
    }

    private static EventFormViewModel ToViewModel(EventDto dto)
    {
        return new EventFormViewModel
        {
            EventId = dto.EventId,
            EventName = dto.EventName,
            Description = dto.Description ?? string.Empty,
            EventType = dto.EventType,
            EventDate = dto.EventDate.Date,
            StartTime = dto.StartTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
            Location = dto.Location,
            EntryFee = dto.EntryFee,
            MaxParticipants = dto.MaxParticipants,
            Status = dto.Status,
            BannerUrl = dto.BannerUrl
        };
    }
    // END: HELPERS
}

// =======================================================
// END: ORGANISER EVENTS CONTROLLER
// =======================================================
