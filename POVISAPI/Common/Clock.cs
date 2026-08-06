namespace POVISAPI.Common
{
    public static class Clock
    {
        public static DateTime PhNow => DateTime.UtcNow.AddHours(8);
    }
}
