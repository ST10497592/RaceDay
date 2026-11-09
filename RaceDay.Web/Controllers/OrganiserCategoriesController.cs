using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// =======================================================
// START: ORGANISER CATEGORIES CONTROLLER
// Purpose: Create, edit and delete race categories.
// =======================================================

[SessionAuthorize("Organiser")]
[Route("organiser/categories/{action=Index}/{id?}")]
public class OrganiserCategoriesController : AppController
{
    public OrganiserCategoriesController(RaceDayApiClient api, UserSession currentUser)
        : base(api, currentUser)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetCategoriesAsync();

        if (!result.Success || result.Data == null)
        {
            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            Fail(result.Error ?? "We could not load the categories.");

            return View(new CategoryListViewModel());
        }

        return View(new CategoryListViewModel
        {
            Categories = result.Data.OrderBy(c => c.CategoryName).ToList()
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new CategoryFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Form", model);

        var result = await Api.CreateCategoryAsync(ToDto(model));

        if (!result.Success)
        {
            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            ModelState.AddModelError(string.Empty, result.IsConflict
                ? "A category with that name already exists."
                : (result.Error ?? "We could not create the category."));

            return View("Form", model);
        }

        Success("Category created.");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await Api.GetCategoryAsync(id);

        if (!result.Success || result.Data == null)
        {
            if (result.IsNotFound)
            {
                Fail("That category could not be found.");

                return RedirectToAction(nameof(Index));
            }

            return FailWith(result, RedirectToAction(nameof(Index)));
        }

        return View("Form", new CategoryFormViewModel
        {
            CategoryId = result.Data.CategoryId,
            CategoryName = result.Data.CategoryName,
            Description = result.Data.Description,
            MinimumAge = result.Data.MinimumAge,
            MaximumAge = result.Data.MaximumAge
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        model.CategoryId = id;

        if (!ModelState.IsValid)
            return View("Form", model);

        var result = await Api.UpdateCategoryAsync(id, ToDto(model));

        if (!result.Success)
        {
            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            ModelState.AddModelError(string.Empty, result.IsConflict
                ? "A category with that name already exists."
                : (result.Error ?? "We could not update the category."));

            return View("Form", model);
        }

        Success("Category updated.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteCategoryAsync(id);

        if (!result.Success)
        {
            if (result.IsConflict)
            {
                Fail("This category is in use by events or enrolments, so it cannot be deleted.");

                return RedirectToAction(nameof(Index));
            }

            return FailWith(result, RedirectToAction(nameof(Index)));
        }

        Success("Category deleted.");

        return RedirectToAction(nameof(Index));
    }

    private static CategoryDto ToDto(CategoryFormViewModel model)
    {
        return new CategoryDto
        {
            CategoryName = model.CategoryName.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            MinimumAge = model.MinimumAge,
            MaximumAge = model.MaximumAge
        };
    }
}

// =======================================================
// END: ORGANISER CATEGORIES CONTROLLER
// =======================================================
