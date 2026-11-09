using System.Security.Claims;

namespace RaceDay.Extensions;

// START: CLAIMS HELPERS
public static class ClaimsPrincipalExtensions
{
    /// <summary>Returns the authenticated user's ID from the JWT, or null if it is missing.</summary>
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(value, out var id))
            return id;

        return null;
    }
}
// END: CLAIMS HELPERS
