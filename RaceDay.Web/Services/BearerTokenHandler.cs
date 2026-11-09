using System.Net.Http.Headers;

namespace RaceDay.Web.Services;

// =======================================================
// START: BEARER TOKEN HANDLER
// Purpose: Attaches the JWT stored in the session to every
// outgoing API request.
// HttpContext is used directly because HttpClient handlers live
// in their own DI scope, separate from the request scope.
// =======================================================

public class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor;

    public BearerTokenHandler(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string? token = null;

        try
        {
            token = _accessor.HttpContext?.Session.GetString(UserSession.TokenKey);
        }
        catch (InvalidOperationException)
        {
            // Session not available for this request - send it anonymously.
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

// =======================================================
// END: BEARER TOKEN HANDLER
// =======================================================
