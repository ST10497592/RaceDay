using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

// =======================================================
// START: SESSION AUTHORIZE FILTER
// Purpose: Protects controllers/actions. Anonymous visitors are sent
// to the login page; signed-in users with the wrong role are sent
// to the Access Denied page.
// Usage: [SessionAuthorize] or [SessionAuthorize("Organiser")]
// =======================================================

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public SessionAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.RequestServices.GetRequiredService<UserSession>();

        if (!session.IsAuthenticated)
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;

            if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
                returnUrl = string.Empty;

            session.Clear();

            context.Result = new RedirectToActionResult(
                "Login",
                "Account",
                new { returnUrl = returnUrl });

            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(session.Role))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}

// =======================================================
// END: SESSION AUTHORIZE FILTER
// =======================================================
