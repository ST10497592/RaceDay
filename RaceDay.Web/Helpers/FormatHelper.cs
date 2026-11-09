using System.Globalization;

namespace RaceDay.Web.Helpers;

// =======================================================
// START: FORMAT HELPER
// Purpose: Display formatting shared by the views.
// =======================================================

public static class FormatHelper
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-ZA");

    public static string Date(DateTime date)
    {
        return date.ToString("ddd, d MMM yyyy", CultureInfo.InvariantCulture);
    }

    public static string LongDate(DateTime date)
    {
        return date.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture);
    }

    public static string ShortDate(DateTime date)
    {
        return date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
    }

    public static string Time(TimeSpan time)
    {
        return time.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
    }

    public static string FinishTime(TimeSpan? time)
    {
        if (!time.HasValue)
            return "-";

        return time.Value.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);
    }

    public static string Money(decimal amount)
    {
        if (amount <= 0)
            return "Free";

        return "R" + amount.ToString("#,##0.00", CultureInfo.InvariantCulture);
    }

    // 1 -> 1st, 2 -> 2nd, 3 -> 3rd, 11 -> 11th, 47 -> 47th
    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;

        if (lastTwo >= 11 && lastTwo <= 13)
            return number + "th";

        switch (number % 10)
        {
            case 1: return number + "st";
            case 2: return number + "nd";
            case 3: return number + "rd";
            default: return number + "th";
        }
    }

    // "47th out of 312 finishers"
    public static string PositionText(int? position, int totalFinishers)
    {
        if (!position.HasValue)
            return "-";

        if (totalFinishers <= 0)
            return Ordinal(position.Value);

        return Ordinal(position.Value) + " out of " + totalFinishers + (totalFinishers == 1 ? " finisher" : " finishers");
    }

    public static string ResultStatusText(string status)
    {
        switch (status)
        {
            case "DidNotFinish": return "Did not finish";
            case "Disqualified": return "Disqualified";
            case "Finished": return "Finished";
            default: return status;
        }
    }

    // CSS class for the coloured status badges (green, amber, red, ...).
    public static string BadgeClass(string? status)
    {
        switch (status)
        {
            case "Confirmed":
            case "Paid":
            case "Open":
            case "Finished":
                return "rd-badge rd-badge-green";
            case "Pending":
            case "Upcoming":
                return "rd-badge rd-badge-amber";
            case "Cancelled":
            case "Failed":
            case "Disqualified":
                return "rd-badge rd-badge-red";
            case "Completed":
            case "Refunded":
                return "rd-badge rd-badge-blue";
            case "Closed":
            case "DidNotFinish":
                return "rd-badge rd-badge-grey";
            default:
                return "rd-badge rd-badge-grey";
        }
    }

    public static string EventTypeIcon(string? type)
    {
        switch (type)
        {
            case "Cycling": return "bi-bicycle";
            case "Walking": return "bi-person-walking";
            default: return "bi-lightning-charge-fill";
        }
    }
}

// =======================================================
// END: FORMAT HELPER
// =======================================================
