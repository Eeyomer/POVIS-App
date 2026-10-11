namespace POVISAPI.Common
{
    public static class Clock
    {
        /// <summary>Current Philippine (Manila, UTC+8, no DST) wall-clock time. Kind is Unspecified so it serializes
        /// without a "Z" and is not shifted again by clients. Persisted timestamps stay in UTC.</summary>
        public static DateTime PhNow => DateTime.SpecifyKind(DateTime.UtcNow.AddHours(8), DateTimeKind.Unspecified);

        /// <summary>Today in Manila.</summary>
        public static DateOnly PhToday => DateOnly.FromDateTime(PhNow);
    }
}
