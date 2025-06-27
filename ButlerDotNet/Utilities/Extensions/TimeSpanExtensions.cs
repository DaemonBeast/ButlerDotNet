namespace ButlerDotNet.Utilities.Extensions;

public static class TimeSpanExtensions
{
    public static string FormatAsMinutes(this TimeSpan timeSpan)
    {
        var minutes = (int) timeSpan.TotalMinutes;
        var seconds = timeSpan.Seconds;

        return minutes == 0 ? $"{seconds}s" : $"{minutes}mins, {seconds}s";
    }
}
