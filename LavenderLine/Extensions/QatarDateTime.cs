using NodaTime;

namespace LavenderLine.Extensions
{
    public static class QatarDateTime
    {
        public static readonly DateTimeZone QatarZone =
            DateTimeZoneProviders.Tzdb["Asia/Qatar"];

        public static Instant Now => SystemClock.Instance.GetCurrentInstant();
    }
}
