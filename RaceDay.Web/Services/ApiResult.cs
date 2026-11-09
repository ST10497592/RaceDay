using System.Net;

namespace RaceDay.Web.Services;

// =======================================================
// START: API RESULT
// Purpose: The outcome of a call to the RaceDay API, with a
// user-friendly error message (never a raw exception).
// =======================================================

public class ApiResult
{
    public bool Success { get; init; }

    public HttpStatusCode StatusCode { get; init; }

    public string? Error { get; init; }

    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;

    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;

    public bool IsConflict => StatusCode == HttpStatusCode.Conflict;
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }

    public static ApiResult<T> Ok(HttpStatusCode statusCode, T? data)
    {
        return new ApiResult<T>
        {
            Success = true,
            StatusCode = statusCode,
            Data = data
        };
    }

    public static ApiResult<T> Failure(HttpStatusCode statusCode, string error)
    {
        return new ApiResult<T>
        {
            Success = false,
            StatusCode = statusCode,
            Error = error
        };
    }
}

// =======================================================
// END: API RESULT
// =======================================================
