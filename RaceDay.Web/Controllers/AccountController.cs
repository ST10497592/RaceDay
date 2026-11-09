using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// =======================================================
// START: ACCOUNT CONTROLLER
// Purpose: Registration, login, logout and profile management.
// All work is delegated to the RaceDay API.
// =======================================================

public class AccountController : AppController
{
    public AccountController(RaceDayApiClient api, UserSession currentUser)
        : base(api, currentUser)
    {
    }

    // START: LOGIN
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (CurrentUser.IsAuthenticated)
            return RedirectToRoleHome();

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await Api.LoginAsync(model.Email.Trim(), model.Password);

        if (!result.Success || result.Data == null)
        {
            var message = result.IsUnauthorized
                ? "Invalid email or password. Please try again."
                : (result.Error ?? "We could not sign you in. Please try again.");

            ModelState.AddModelError(string.Empty, message);
            model.Password = string.Empty;

            return View(model);
        }

        CurrentUser.SignIn(result.Data);
        Success("Welcome back, " + result.Data.User.FirstName + "!");

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            // Organisers must not land on participant-only pages and vice versa;
            // the role filters handle that, so a local return URL is safe to follow.
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToRoleHome();
    }
    // END: LOGIN

    // START: REGISTER
    [HttpGet]
    public IActionResult Register(string? role = null)
    {
        if (CurrentUser.IsAuthenticated)
            return RedirectToRoleHome();

        var model = new RegisterViewModel();

        if (role == "Organiser")
            model.Role = "Organiser";

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role == "Organiser" && string.IsNullOrWhiteSpace(model.OrganiserCode))
        {
            ModelState.AddModelError(nameof(model.OrganiserCode), "Organisers must enter the organiser access code.");
        }

        if (model.DateOfBirth.HasValue && model.DateOfBirth.Value.Date >= DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth must be in the past.");
        }

        if (!ModelState.IsValid)
            return View(PrepareForRedisplay(model));

        var request = new RegisterApiRequest
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = model.Email.Trim(),
            Password = model.Password,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            DateOfBirth = model.DateOfBirth,
            Role = model.Role,
            OrganiserCode = model.Role == "Organiser" ? model.OrganiserCode?.Trim() : null
        };

        var result = await Api.RegisterAsync(request);

        if (!result.Success)
        {
            var message = result.IsConflict
                ? "An account with that email address already exists. Try signing in instead."
                : result.IsForbidden
                    ? "That organiser access code is not valid."
                    : (result.Error ?? "We could not create your account. Please try again.");

            ModelState.AddModelError(string.Empty, message);

            return View(PrepareForRedisplay(model));
        }

        // Sign the new user in straight away.
        var login = await Api.LoginAsync(request.Email, request.Password);

        if (login.Success && login.Data != null)
        {
            CurrentUser.SignIn(login.Data);
            Success("Your account has been created. Welcome to RaceDay, " + login.Data.User.FirstName + "!");

            return RedirectToRoleHome();
        }

        Success("Your account has been created. Please sign in.");

        return RedirectToAction(nameof(Login));
    }

    private static RegisterViewModel PrepareForRedisplay(RegisterViewModel model)
    {
        model.Password = string.Empty;
        model.ConfirmPassword = string.Empty;

        return model;
    }
    // END: REGISTER

    // START: LOGOUT
    [HttpPost]
    [SessionAuthorize]
    public async Task<IActionResult> Logout()
    {
        await Api.LogoutAsync();

        CurrentUser.Clear();
        Success("You have been signed out.");

        return RedirectToAction("Index", "Home");
    }
    // END: LOGOUT

    // START: PROFILE
    [HttpGet]
    [SessionAuthorize]
    public async Task<IActionResult> Profile()
    {
        var result = await Api.GetProfileAsync();

        if (!result.Success || result.Data == null)
            return FailWith(result, RedirectToRoleHome());

        return View(ToViewModel(result.Data));
    }

    [HttpPost]
    [SessionAuthorize]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (model.DateOfBirth.HasValue && model.DateOfBirth.Value.Date >= DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth must be in the past.");
        }

        if (!ModelState.IsValid)
        {
            await FillReadOnlyDetails(model);
            return View(model);
        }

        var result = await Api.UpdateProfileAsync(
            model.FirstName.Trim(),
            model.LastName.Trim(),
            string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            model.DateOfBirth);

        if (!result.Success)
        {
            var expired = HandleExpiredSession(result);

            if (expired != null)
                return expired;

            ModelState.AddModelError(string.Empty, result.Error ?? "We could not update your profile.");
            await FillReadOnlyDetails(model);

            return View(model);
        }

        CurrentUser.UpdateName(model.FirstName.Trim(), model.LastName.Trim());
        Success("Your profile has been updated.");

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [SessionAuthorize]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        var problem = ValidateImage(picture);

        if (problem != null)
        {
            Fail(problem);
            return RedirectToAction(nameof(Profile));
        }

        var result = await Api.UploadProfilePictureAsync(picture!);

        if (!result.Success || result.Data == null)
            return FailWith(result, RedirectToAction(nameof(Profile)));

        CurrentUser.UpdatePicture(result.Data.Url);
        Success("Your profile picture has been updated.");

        return RedirectToAction(nameof(Profile));
    }

    private static ProfileViewModel ToViewModel(ProfileDto dto)
    {
        return new ProfileViewModel
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            DateOfBirth = dto.DateOfBirth,
            Email = dto.Email,
            Role = dto.Role,
            ProfilePictureUrl = dto.ProfilePictureUrl
        };
    }

    // The read-only fields are not posted back, so they are reloaded when the form is redisplayed.
    private async Task FillReadOnlyDetails(ProfileViewModel model)
    {
        var profile = await Api.GetProfileAsync();

        if (profile.Success && profile.Data != null)
        {
            model.Email = profile.Data.Email;
            model.Role = profile.Data.Role;
            model.ProfilePictureUrl = profile.Data.ProfilePictureUrl;
        }
    }
    // END: PROFILE

    // START: ACCESS DENIED
    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;

        return View();
    }
    // END: ACCESS DENIED
}

// =======================================================
// END: ACCOUNT CONTROLLER
// =======================================================
