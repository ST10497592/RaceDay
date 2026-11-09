using System.Globalization;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

// =======================================================
// START: USER SESSION
// Purpose: Reads and writes the signed-in user's details
// (JWT, id, role, name, picture) in the server-side session.
// =======================================================

public class UserSession
{
    public const string TokenKey = "raceday.jwt";
    private const string ExpiresKey = "raceday.expires";
    private const string UserIdKey = "raceday.userId";
    private const string RoleKey = "raceday.role";
    private const string FirstNameKey = "raceday.firstName";
    private const string LastNameKey = "raceday.lastName";
    private const string EmailKey = "raceday.email";
    private const string PictureKey = "raceday.picture";

    private readonly IHttpContextAccessor _accessor;

    public UserSession(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ISession? Store
    {
        get
        {
            var context = _accessor.HttpContext;

            if (context == null)
                return null;

            try
            {
                return context.Session;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }

    public string? Token => Store?.GetString(TokenKey);

    public bool IsAuthenticated
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Token))
                return false;

            var expiresText = Store?.GetString(ExpiresKey);

            if (long.TryParse(expiresText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
            {
                if (new DateTime(ticks, DateTimeKind.Utc) <= DateTime.UtcNow)
                    return false;
            }

            return true;
        }
    }

    public int? UserId
    {
        get
        {
            var value = Store?.GetString(UserIdKey);

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                return id;

            return null;
        }
    }

    public string Role => IsAuthenticated ? (Store?.GetString(RoleKey) ?? string.Empty) : string.Empty;

    public bool IsOrganiser => Role == "Organiser";

    public bool IsParticipant => Role == "Participant";

    public string FirstName => Store?.GetString(FirstNameKey) ?? string.Empty;

    public string LastName => Store?.GetString(LastNameKey) ?? string.Empty;

    public string FullName => (FirstName + " " + LastName).Trim();

    public string Email => Store?.GetString(EmailKey) ?? string.Empty;

    public string? ProfilePictureUrl => Store?.GetString(PictureKey);

    public string Initials
    {
        get
        {
            var first = FirstName.Length > 0 ? FirstName.Substring(0, 1) : string.Empty;
            var last = LastName.Length > 0 ? LastName.Substring(0, 1) : string.Empty;
            return (first + last).ToUpperInvariant();
        }
    }

    // START: SIGN IN
    public void SignIn(LoginResponseDto login)
    {
        var store = Store;

        if (store == null)
            return;

        store.Clear();
        store.SetString(TokenKey, login.Token);
        store.SetString(ExpiresKey, login.ExpiresAt.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture));
        store.SetString(UserIdKey, login.User.UserId.ToString(CultureInfo.InvariantCulture));
        store.SetString(RoleKey, login.User.Role);
        store.SetString(FirstNameKey, login.User.FirstName);
        store.SetString(LastNameKey, login.User.LastName);
        store.SetString(EmailKey, login.User.Email);
        store.SetString(PictureKey, login.User.ProfilePictureUrl ?? string.Empty);
    }
    // END: SIGN IN

    public void UpdateName(string firstName, string lastName)
    {
        Store?.SetString(FirstNameKey, firstName);
        Store?.SetString(LastNameKey, lastName);
    }

    public void UpdatePicture(string? url)
    {
        Store?.SetString(PictureKey, url ?? string.Empty);
    }

    public void Clear()
    {
        Store?.Clear();
    }
}

// =======================================================
// END: USER SESSION
// =======================================================
