using System;

namespace NebulaSoft
{
    public static class TimeUtils
    {
        private static readonly DateTime DEFAULT_DATE_UNIX = new DateTime(1970, 1, 1, 0, 0, 0);

        public const string FORMAT_FULL = "d/M/yyyy HH:mm:ss";
        public const string FORMAT_SHORT = "t";

        /// <summary>
        /// Returns the current Unix timestamp, which represents the number of seconds that have elapsed
        /// since January 1, 1970 (the Unix epoch).
        /// </summary>
        /// <returns>The current Unix timestamp as a double.</returns>
        public static double GetCurrentUnixTimestamp()
        {
            double unixTimestamp = (DateTime.Now - DEFAULT_DATE_UNIX).TotalSeconds;

            return unixTimestamp;
        }

        /// <summary>
        /// Returns the current Unix timestamp with optional additional time added.
        /// The additional time can be specified in weeks, days, hours, minutes, and seconds.
        /// </summary>
        /// <param name="weeks">The number of weeks to add (default is 0).</param>
        /// <param name="days">The number of days to add (default is 0).</param>
        /// <param name="hours">The number of hours to add (default is 0).</param>
        /// <param name="minutes">The number of minutes to add (default is 0).</param>
        /// <param name="seconds">The number of seconds to add (default is 0).</param>
        /// <returns>The Unix timestamp with the specified extra time as a double.</returns>
        public static double GetCurrentUnixTimestampWithExtraTime(int weeks = 0, int days = 0, int hours = 0, int minutes = 0, int seconds = 0)
        {
            DateTime nowDate = DateTime.Now;
            nowDate = nowDate.AddDays(weeks * 7);
            nowDate = nowDate.AddDays(days);
            nowDate = nowDate.AddHours(hours);
            nowDate = nowDate.AddMinutes(minutes);
            nowDate = nowDate.AddSeconds(seconds);

            double unixTimestamp = (nowDate - DEFAULT_DATE_UNIX).TotalSeconds;

            return unixTimestamp;
        }

        /// <summary>
        /// Returns the Unix timestamp for the start of the current day (midnight).
        /// This is the number of seconds since January 1, 1970, for the current day's midnight time.
        /// </summary>
        /// <returns>The Unix timestamp for the start of the current day as a double.</returns>
        public static double GetCurrentDayUnixTimestamp()
        {
            DateTime nowDate = DateTime.Now;
            nowDate = nowDate.AddHours(-nowDate.Hour);
            nowDate = nowDate.AddMinutes(-nowDate.Minute);
            nowDate = nowDate.AddSeconds(-nowDate.Second);

            double unixTimestamp = (nowDate - DEFAULT_DATE_UNIX).TotalSeconds;

            return unixTimestamp;
        }

        /// <summary>
        /// Converts a given DateTime to a Unix timestamp, which is the number of seconds that have elapsed
        /// since January 1, 1970, in the specified DateTime's time zone.
        /// </summary>
        /// <param name="target">The target DateTime to convert to a Unix timestamp.</param>
        /// <returns>The Unix timestamp as a double.</returns>
        public static double GetUnixTimestampFromDateTime(DateTime target)
        {
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, target.Kind);

            double unixTimestamp = (target - date).TotalSeconds;

            return unixTimestamp;
        }

        /// <summary>
        /// Converts a Unix timestamp to a DateTime object representing the date and time
        /// since January 1, 1970 (the Unix epoch).
        /// </summary>
        /// <param name="unixTime">The Unix timestamp as a double.</param>
        /// <returns>A DateTime object corresponding to the given Unix timestamp.</returns>
        public static DateTime GetDateTimeFromUnixTime(double unixTime)
        {
            return DEFAULT_DATE_UNIX.AddSeconds(unixTime);
        }

        /// <summary>
        /// Returns the current date and time as a string based on the provided format.
        /// The format string can contain standard or custom date and time format specifiers
        /// as supported by the DateTime.ToString() method.
        /// Example format strings:
        /// - "yyyy-MM-dd" => "2024-09-17"
        /// - "HH:mm:ss" => "14:30:45"
        /// - "MMMM dd, yyyy" => "September 17, 2024"
        /// </summary>
        /// <param name="format">A string representing the desired date and time format.</param>
        /// <returns>The current date and time as a formatted string.</returns>
        public static string GetCurrentDateString(string format)
        {
            return DateTime.Now.ToString(format);
        }

        /// <summary>
        /// Formats the time based on the provided duration in minutes and a custom format rule.
        /// The format rule can contain the following placeholders:
        /// - {mm}: Total minutes (rounded down)
        /// - {ss}: Total seconds
        /// - {hh}: Total hours (rounded down)
        /// - {s}: Remaining seconds
        /// - {m}: Remaining minutes (after extracting hours)
        /// - {h}: Remaining hours (within a 24-hour period)
        /// Example format rules: 
        /// - "{mm}mins" => "65mins"
        /// - "{hh}hrs" => "1hrs"
        /// - "{h}:{m}" => "1:5"
        /// </summary>
        /// <param name="durationInMinutes">The total time duration in minutes.</param>
        /// <param name="formatRule">The format string that includes placeholders to be replaced.</param>
        /// <returns>The formatted time string based on the format rule.</returns>
        public static string GetFormatedTime(float durationInMinutes, string formatRule)
        {
            // Convert the duration to TimeSpan
            TimeSpan timeSpan = TimeSpan.FromMinutes(durationInMinutes);

            // Replace formatRule placeholders with actual values
            return formatRule
                .Replace("{ss}", ((int)timeSpan.TotalSeconds).ToString())
                .Replace("{mm}", ((int)timeSpan.TotalMinutes).ToString())
                .Replace("{hh}", ((int)timeSpan.TotalHours).ToString())
                .Replace("{s}", timeSpan.Seconds.ToString())
                .Replace("{m}", timeSpan.Minutes.ToString())
                .Replace("{h}", timeSpan.Hours.ToString());
        }
    }
}