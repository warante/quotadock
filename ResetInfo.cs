using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace UsageTray
{
    public sealed class ResetInfo
    {
        public readonly DateTimeOffset? At;
        public readonly DateTime? DateOnly;
        public readonly bool Relative;
        private static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        private ResetInfo(DateTimeOffset? at, DateTime? dateOnly, bool relative) { At = at; DateOnly = dateOnly; Relative = relative; }

        public static ResetInfo Iso(object value)
        {
            string text = Json.Text(value);
            DateTimeOffset timestamp;
            if (text.Contains("T") && Regex.IsMatch(text, @"(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.IgnoreCase) &&
                DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp))
                return new ResetInfo(timestamp, null, false);
            DateTime date;
            if (DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return new ResetInfo(null, date.Date, false);
            return null;
        }
        public static ResetInfo Unix(object value, bool milliseconds)
        {
            var number = Json.Number(value);
            if (!number.HasValue || number <= 0 || Math.Truncate(number.Value) != number) return null;
            // Units are known per field. Reject the opposite unit instead of showing a bogus date.
            if (milliseconds ? number < 100000000000 : number >= 100000000000) return null;
            try { return new ResetInfo(milliseconds ? Epoch.AddMilliseconds(number.Value) : Epoch.AddSeconds(number.Value), null, false); }
            catch (ArgumentOutOfRangeException) { return null; }
        }
        public static ResetInfo After(object seconds, DateTimeOffset receivedAt)
        {
            var number = Json.Number(seconds);
            if (!number.HasValue || number < 0 || number > 366 * 86400 || Math.Truncate(number.Value) != number) return null;
            try { return new ResetInfo(receivedAt.AddSeconds(number.Value), null, true); }
            catch (ArgumentOutOfRangeException) { return null; }
        }
        public static ResetInfo Kimi(object bucket)
        {
            return Iso(Json.Get(bucket, "resetTime") ?? Json.Get(bucket, "resetAt") ?? Json.Get(bucket, "reset_at") ?? Json.Get(bucket, "reset_time"));
        }
        public static string Compact(ResetInfo reset, bool showTime, DateTimeOffset now)
        {
            if (reset == null) return "—";
            var today = TimeZoneInfo.ConvertTime(now, Madrid).Date;
            if (reset.At.HasValue)
            {
                if (reset.At.Value <= now) return "—";
                var local = TimeZoneInfo.ConvertTime(reset.At.Value, Madrid);
                return (reset.Relative ? "≈" : "") + local.ToString(showTime ? "HH:mm" : "dd/MM", CultureInfo.InvariantCulture);
            }
            if (reset.DateOnly.Value < today || showTime) return "—";
            return reset.DateOnly.Value.ToString("dd/MM", CultureInfo.InvariantCulture);
        }
        public static string Describe(ResetInfo reset, DateTimeOffset now)
        {
            if (reset == null) return "El proveedor no comunica una hora de reinicio válida.";
            if (reset.At.HasValue)
            {
                if (reset.At.Value <= now) return "Reinicio anterior; pendiente de actualizar. No se conoce el próximo.";
                var local = TimeZoneInfo.ConvertTime(reset.At.Value, Madrid);
                string zone = Madrid.IsDaylightSavingTime(reset.At.Value) ? "CEST" : "CET";
                return (reset.Relative ? "Reinicio aproximado: " : "Reinicio comunicado: ") +
                    local.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " " + zone + " (hora peninsular española).";
            }
            var today = TimeZoneInfo.ConvertTime(now, Madrid).Date;
            if (reset.DateOnly.Value < today) return "Fecha de reinicio anterior; pendiente de actualizar.";
            return "Fecha comunicada: " + reset.DateOnly.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + " (el proveedor no indica hora ni zona).";
        }
    }
}
