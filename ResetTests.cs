using System;
using System.Linq;
using UsageTray;

internal static class ResetTests
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        check(ResetInfo.Compact(ResetInfo.Iso("2026-01-01T10:30:00Z"), true, now) == "11:30", "Inline session time uses Madrid");
        check(ResetInfo.Compact(ResetInfo.Iso("2026-06-12T10:30:00Z"), true, now) == "12:30", "Inline session shows only summer time");
        check(ResetInfo.Compact(null, true, now) == "—", "Inline missing reset");
        check(ResetInfo.Compact(ResetInfo.Iso("2025-12-31T10:30:00Z"), false, now) == "—", "Inline expired reset");
        check(ResetInfo.Compact(ResetInfo.Iso("2026-06-12"), true, now) == "—", "Inline date-only reset never invents time");
        check(ResetInfo.Compact(ResetInfo.After(3600, now), true, now).Contains("≈"), "Inline relative reset marked approximate");
        check(ResetInfo.Compact(ResetInfo.Iso("2026-01-01T23:30:00Z"), false, now) == "02/01", "Inline weekly date uses Madrid day");
        var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var target = DateTimeOffset.Parse("2030-06-12T10:30:00Z");
        long seconds = (long)(target - epoch).TotalSeconds;
        long milliseconds = seconds * 1000;
        check(ResetInfo.Unix(seconds, false).At == target && ResetInfo.Unix(milliseconds, true).At == target, "Reset timestamp units");
        check(ResetInfo.Unix(seconds, true) == null && ResetInfo.Unix(milliseconds, false) == null, "Reject swapped seconds and milliseconds");
        foreach (object invalid in new object[] { null, 0, -1, double.NaN, double.PositiveInfinity, 123.5, "garbage" })
            check(ResetInfo.Unix(invalid, false) == null, "Invalid reset timestamp");
        foreach (string invalid in new[] { "later", "2026-99-99", "2030-01-01T00:00:00", "2030-01-01 10:00" })
            check(ResetInfo.Iso(invalid) == null, "No local-time guessing");
        check(ResetInfo.Iso("2030-06-12T12:30:00+02:00").At == target, "Explicit offset interpreted correctly");
        check(ResetInfo.After(3600, now).At == now.AddHours(1) && ResetInfo.After(3600, now).Relative, "Relative reset anchored to observation");
        check(ResetInfo.After(-1, now) == null && ResetInfo.After(double.MaxValue, now) == null, "Invalid relative reset");
        check(ResetInfo.Describe(null, now).Contains("no comunica"), "Missing reset is not invented");
        check(ResetInfo.Describe(ResetInfo.Iso("2025-12-31T23:59:59Z"), now).Contains("pendiente"), "Past reset not presented as next");
        check(ResetInfo.Describe(ResetInfo.Iso("2025-12-30"), now).Contains("anterior"), "Stale date-only reset");
        check(ResetInfo.Describe(ResetInfo.Iso("2030-06-12"), now).Contains("no indica hora ni zona"), "Date-only does not become midnight");
        check(ResetInfo.Describe(ResetInfo.After(3600, now), now).Contains("aproximado"), "Relative reset is labelled approximate");
        check(ResetInfo.Describe(ResetInfo.Iso("2026-03-29T00:30:00Z"), now).Contains("29/03/2026 01:30 CET"), "Madrid before spring transition");
        check(ResetInfo.Describe(ResetInfo.Iso("2026-03-29T01:30:00Z"), now).Contains("29/03/2026 03:30 CEST"), "Madrid after spring transition");
        check(ResetInfo.Describe(ResetInfo.Iso("2026-10-25T00:30:00Z"), now).Contains("25/10/2026 02:30 CEST"), "Madrid first repeated autumn hour");
        check(ResetInfo.Describe(ResetInfo.Iso("2026-10-25T01:30:00Z"), now).Contains("25/10/2026 02:30 CET"), "Madrid second repeated autumn hour");
        var codex = Usage.Parse("codex", "{\"rate_limit\":{\"primary_window\":{\"used_percent\":1,\"reset_at\":" + seconds + "}}}", now);
        check(codex.Meters[0].Reset.At == target, "Codex absolute reset");
        var relative = Usage.Parse("codex", "{\"rate_limit\":{\"primary_window\":{\"used_percent\":1,\"reset_after_seconds\":600}}}", now);
        check(relative.Meters[0].Reset.At == now.AddMinutes(10), "Codex relative fallback");
        var old = Usage.Parse("codex", "{\"rate_limit\":{\"primary_window\":{\"used_percent\":1,\"reset_at\":1,\"reset_after_seconds\":600}}}", now);
        check(ResetInfo.Describe(old.Meters[0].Reset, now).Contains("pendiente"), "Old absolute reset is not silently rolled forward");
        check(Usage.Parse("claude", "{\"five_hour\":{\"utilization\":1,\"resets_at\":\"2030-06-12T10:30:00Z\"}}").Meters[0].Reset.At == target, "Claude reset");
        check(Usage.Parse("go", "{\"usage\":{\"weekly\":{\"status\":\"ok\",\"percent\":1,\"resetsAt\":\"2030-06-12T10:30:00Z\"}}}").Meters[0].Reset.At == target, "Go reset");
        check(Usage.Parse("kimi", "{\"usage\":{\"limit\":100,\"used\":1,\"resetTime\":\"2030-06-12T10:30:00Z\"}}").Meters.Last().Reset.At == target, "Kimi weekly reset");
        check(Usage.Parse("kimi", "{\"usages\":{\"limit_month_total\":{\"used_ratio\":0.1,\"reset_time\":\"2030-06-12T10:30:00Z\"}}}").Meters.Last().Reset.At == target, "Kimi monthly reset");
        check(Usage.Parse("minimax", "{\"base_resp\":{\"status_code\":0},\"model_remains\":[{\"model_name\":\"general\",\"current_interval_remaining_percent\":90,\"end_time\":" + milliseconds + ",\"weekly_end_time\":" + milliseconds + "}]}").Meters[0].Reset.At == target, "MiniMax millisecond reset");
        check(Usage.Parse("zai", "{\"success\":true,\"data\":{\"limits\":[{\"type\":\"CREDIT_LIMIT\",\"unit\":3,\"percentage\":1,\"nextResetTime\":" + milliseconds + "}]}}").Meters[0].Reset.At == target, "ZAI millisecond reset");
        check(Usage.Parse("copilot", "{\"quota_reset_date\":\"2030-06-12\",\"quota_snapshots\":{\"premium_interactions\":{\"percent_remaining\":90}}}").Meters[0].Reset.At == null, "Copilot date-only remains date-only");
        var gemini = Usage.Parse("gemini", "{\"buckets\":[{\"modelId\":\"gemini-pro\",\"remainingFraction\":0.8,\"resetTime\":\"2030-06-12T09:00:00Z\"},{\"modelId\":\"gemini-pro\",\"remainingFraction\":0.6,\"resetTime\":\"2030-06-12T10:30:00Z\"}]}");
        check(gemini.Meters[0].Reset.At == target, "Gemini reset belongs to limiting bucket");
        var ambiguous = Usage.Parse("gemini", "{\"buckets\":[{\"modelId\":\"gemini-pro\",\"remainingFraction\":0.6,\"resetTime\":\"2030-06-12T09:00:00Z\"},{\"modelId\":\"gemini-pro\",\"remainingFraction\":0.6,\"resetTime\":\"2030-06-12T10:30:00Z\"}]}");
        check(ambiguous.Meters[0].Reset == null, "No single reset for ambiguous family aggregate");
    }
}
