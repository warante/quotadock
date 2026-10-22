using System;
using System.Net;
using System.Net.Http;
using UsageTray;

internal static class Tests
{
    private static void Equal(double? actual, double? expected)
    {
        if (actual != expected) throw new Exception("Expected " + expected + ", got " + actual);
    }
    public static int Main(string[] args)
    {
        try
        {
#if !CLAUDE
            var normal = Usage.Parse("{\"rate_limit\":{\"primary_window\":{\"used_percent\":23,\"limit_window_seconds\":18000},\"secondary_window\":{\"used_percent\":64.5,\"limit_window_seconds\":604800}}}");
            Equal(normal.FiveHours, 77); Equal(normal.Week, 35.5);
            var swapped = Usage.Parse("{\"rate_limit\":{\"primary_window\":{\"used_percent\":70,\"limit_window_seconds\":604800},\"secondary_window\":null}}");
            Equal(swapped.FiveHours, null); Equal(swapped.Week, 30);
            var clamp = Usage.Parse("{\"rate_limit\":{\"primary_window\":{\"used_percent\":120},\"secondary_window\":{\"used_percent\":-2}}}");
            Equal(clamp.FiveHours, 0); Equal(clamp.Week, 100);
            var missing = Usage.Parse("{\"rate_limit\":{\"primary_window\":{\"used_percent\":null},\"secondary_window\":{\"used_percent\":\"NaN\"}}}");
            Equal(missing.FiveHours, null); Equal(missing.Week, null);
            var other = Usage.Parse("{\"rate_limit\":{\"primary_window\":{\"used_percent\":10,\"limit_window_seconds\":3600}}}");
            Equal(other.FiveHours, null); Equal(other.Week, null);
            Equal(Usage.Parse("{}").FiveHours, null);
#endif
            var claude = Usage.ParseClaude("{\"five_hour\":{\"utilization\":6.5},\"seven_day\":{\"utilization\":35}}");
            Equal(claude.FiveHours, 93.5); Equal(claude.Week, 65);
            var claudeMissing = Usage.ParseClaude("{\"five_hour\":null,\"seven_day\":{\"utilization\":null}}");
            Equal(claudeMissing.FiveHours, null); Equal(claudeMissing.Week, null);
            var claudeInvalid = Usage.ParseClaude("{\"five_hour\":{\"utilization\":\"NaN\"},\"seven_day\":{\"utilization\":\"Infinity\"}}");
            Equal(claudeInvalid.FiveHours, null); Equal(claudeInvalid.Week, null);
            var claudeClamp = Usage.ParseClaude("{\"five_hour\":{\"utilization\":105},\"seven_day\":{\"utilization\":-5}}");
            Equal(claudeClamp.FiveHours, 0); Equal(claudeClamp.Week, 100);
            Equal(Usage.ParseClaude("{}").Week, null);
#if CLAUDE
            Equal(Usage.Parse("{\"five_hour\":{\"utilization\":12}}").FiveHours, 88);
#endif
            Console.WriteLine("OK: parsing, durations, weekly-only, clamping, missing data, invalid numbers.");
            if (args.Length > 0 && args[0] == "--live")
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    var live = Usage.Fetch(client).GetAwaiter().GetResult();
                    Console.WriteLine("Live: 5 h " + Usage.Percent(live.FiveHours) + "; semana " + Usage.Percent(live.Week));
                }
            }
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
    }
}
