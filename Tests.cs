using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UsageTray;

internal static class Tests
{
    private static int checks;
    private static void Check(bool ok, string label) { checks++; if (!ok) throw new Exception(label); }
    private static double? Value(Snapshot data, string id) { var m = data.Meters.FirstOrDefault(x => x.Id == id); return m == null ? null : m.Remaining; }
    private static void Reject(Action action, string label) { bool rejected = false; try { action(); } catch (QuotaException) { rejected = true; } Check(rejected, label); }
    private sealed class Credentials : CredentialStore
    {
        public Credentials(string path) : base(path) { }
        public override Task<AuthData> Read(ProviderDefinition p) { return Task.FromResult(new AuthData("test-credential", "test-account")); }
    }
    private sealed class Handler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) { return Task.FromResult(Respond(request)); }
    }
    public static int Main(string[] args)
    {
        string root = Path.Combine(Environment.GetEnvironmentVariable("OPENCODE_TEST_TEMP") ?? Path.GetTempPath(), "quotadock-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var normal = Usage.Parse("codex", "{\"rate_limit\":{\"primary_window\":{\"used_percent\":23,\"limit_window_seconds\":18000},\"secondary_window\":{\"used_percent\":64.5,\"limit_window_seconds\":604800}}}");
            Check(Value(normal, "session") == 77 && Value(normal, "weekly") == 35.5, "Codex percentages");
            var weekly = Usage.Parse("codex", "{\"rate_limit\":{\"primary_window\":{\"used_percent\":70,\"limit_window_seconds\":604800},\"secondary_window\":null}}");
            Check(weekly.Visible(false)[0].Remaining == null && weekly.Visible(false)[1].Remaining == 30, "Weekly-only must not become five hours");
            var clamp = Usage.Parse("codex", "{\"rate_limit\":{\"primary_window\":{\"used_percent\":120},\"secondary_window\":{\"used_percent\":-2}}}");
            Check(Value(clamp, "session") == 0 && Value(clamp, "weekly") == 100, "Clamp");
            Reject(() => Usage.Parse("codex", "{}"), "No guessed usage");
            Reject(() => Usage.Parse("claude", "{\"five_hour\":{\"utilization\":\"NaN\"}}"), "NaN");
            var claude = Usage.Parse("claude", "{\"five_hour\":{\"utilization\":6.5},\"seven_day\":{\"utilization\":35}}");
            Check(Value(claude, "session") == 93.5 && Value(claude, "weekly") == 65, "Claude units");
            var go = Usage.Parse("go", "{\"usage\":{\"rolling\":{\"status\":\"ok\",\"percent\":12},\"weekly\":{\"status\":\"rate-limited\",\"percent\":100},\"monthly\":{\"status\":\"ok\",\"percent\":25}}}");
            Check(Value(go, "session") == 88 && go.Visible(true)[1].Remaining == 75, "Go month selection");
            Reject(() => Usage.Parse("go", "{\"usage\":{\"rolling\":{\"percent\":12}}}"), "Go status validation");
            Reject(() => Usage.Parse("go", "{\"error\":\"test\",\"usage\":{}}"), "Go error envelope");
            var kimi = Usage.Parse("kimi", "{\"usage\":{\"limit\":\"100\",\"remaining\":\"74\"},\"limits\":[{\"window\":{\"duration\":300,\"timeUnit\":\"TIME_UNIT_MINUTE\"},\"detail\":{\"limit\":100,\"used\":15}}]}");
            Check(Value(kimi, "session") == 85 && Value(kimi, "weekly") == 74, "Kimi counters");
            var kimiNew = Usage.Parse("kimi", "{\"usages\":{\"limit_month_total\":{\"used_ratio\":0.4},\"limit_5h\":{\"used_ratio\":0.9}}}");
            Check(Value(kimiNew, "weekly") == null && kimiNew.Visible(false)[1].Label == "Mes" && kimiNew.Visible(false)[1].Remaining == 60, "Kimi monthly not weekly or five hour");
            var mini = Usage.Parse("minimax", "{\"base_resp\":{\"status_code\":0},\"model_remains\":[{\"model_name\":\"video\",\"current_interval_remaining_percent\":10},{\"model_name\":\"general\",\"current_interval_remaining_percent\":99,\"current_weekly_remaining_percent\":55}]}");
            Check(Value(mini, "session") == 99 && Value(mini, "weekly") == 55, "MiniMax is remaining, not used");
            Reject(() => Usage.Parse("minimax", "{\"base_resp\":{\"status_code\":1004,\"status_msg\":\"test-private-response\"}}"), "MiniMax in-band auth");
            try { Usage.Parse("minimax", "{\"base_resp\":{\"status_code\":9001,\"status_msg\":\"test-private-response\"}}"); }
            catch (QuotaException ex) { Check(!ex.Message.Contains("test-private-response"), "Error redaction"); }
            var zai = Usage.Parse("zai", "{\"success\":true,\"data\":{\"limits\":[{\"type\":\"CREDIT_LIMIT\",\"unit\":6,\"percentage\":10},{\"type\":\"TOKENS_LIMIT\",\"unit\":3,\"percentage\":42},{\"type\":\"TIME_LIMIT\",\"unit\":5,\"percentage\":99}]}}");
            Check(Value(zai, "session") == 58 && Value(zai, "weekly") == 90 && Value(zai, "monthly") == null, "ZAI unit sorting, MCP not coding quota");
            Reject(() => Usage.Parse("zai", "{\"code\":1000}"), "ZAI HTTP200 failure");
            Reject(() => Usage.Parse("zai", "{\"success\":true,\"data\":{\"limits\":[{\"type\":\"TOKENS_LIMIT\",\"unit\":3,\"percentage\":10},{\"type\":\"CREDIT_LIMIT\",\"unit\":3,\"percentage\":20}]}}"), "ZAI duplicates");
            var copilot = Usage.Parse("copilot", "{\"quota_snapshots\":{\"premium_interactions\":{\"entitlement\":300,\"remaining\":45},\"chat\":{\"unlimited\":true}}}");
            Check(Value(copilot, "premium") == 15 && Value(copilot, "chat") == null && copilot.Visible(false)[0].Label == "Premium/mes", "Copilot monthly quotas");
            var gemini = Usage.Parse("gemini", "{\"buckets\":[{\"modelId\":\"gemini-pro\",\"remainingFraction\":0.8},{\"modelId\":\"gemini-pro\",\"remainingFraction\":0.6},{\"modelId\":\"gemini-flash\",\"remainingFraction\":0.9},{\"modelId\":\"gemini-flash-lite\",\"remainingFraction\":0.1}]}");
            Check(Value(gemini, "pro") == 60 && Value(gemini, "flash") == 90, "Gemini groups and units");
            var now = DateTimeOffset.UtcNow;
            var retry = new RetryPolicy(); retry.Fail(now, 60, new QuotaException("throttle", "test", TimeSpan.FromHours(4)));
            Check(!retry.CanFetch(now.AddHours(3)) && retry.CanFetch(now.AddHours(4)), "Retry-After cannot be bypassed");
            retry.Success(now, 60); Check(retry.Failures == 0 && !retry.CanFetch(now.AddSeconds(59)), "Success cooldown");
            var gate = new AlertGate(); var meter = new Meter("session", "5 h", 10);
            Check(gate.Observe(meter, 10) && !gate.Observe(meter, 10), "No duplicate alert");
            meter.Remaining = 11; Check(!gate.Observe(meter, 10), "Hysteresis");
            meter.Remaining = 30; gate.Observe(meter, 10); meter.Remaining = 9;
            Check(gate.Observe(meter, 10), "New cycle alert");
            var settings = new SettingsStore(root); settings.Save(new Preferences { Enabled = new[] { "kimi", "claude" }, Alerts = true, Threshold = 20 });
            Check(settings.Load().Enabled.Length == 2 && settings.Load().Threshold == 20, "Preferences roundtrip");
            Check(settings.Load().ResolveSelection() == "kimi", "Migration without selected provider picks an enabled account");
            settings.Save(new Preferences { Enabled = new[] { "openai", "claude" }, SelectedProvider = "claude" });
            Check(settings.Load().ResolveSelection() == "claude", "Selected provider persists");
            var selection = new Preferences { Enabled = new[] { "openai", "kimi" }, SelectedProvider = "claude" };
            Check(selection.ResolveSelection() == "openai", "Disabling selected provider picks enabled fallback");
            selection.Enabled = new string[0]; Check(selection.ResolveSelection() == null, "No enabled providers still has a control state");
            selection.Enabled = new[] { "invalid", "kimi" }; Check(selection.ResolveSelection() == "kimi", "Unknown provider cannot be selected");
            settings.SaveSchedules(new Dictionary<string, RetryPolicy> { { "claude", retry } });
            Check(settings.LoadSchedules()["claude"].Next == retry.Next, "Cooldown survives application restart");
            var vault = new CredentialStore(root); var p = Providers.Find("kimi");
            const string sample = "synthetic-test-credential-not-real";
            vault.Save(p, sample);
            Check(!System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(root, "credentials", "kimi.bin"))).Contains(sample), "DPAPI ciphertext, no plaintext");
            string env = Environment.GetEnvironmentVariable("KIMI_API_KEY");
            try { Environment.SetEnvironmentVariable("KIMI_API_KEY", null); Check(vault.Read(p).GetAwaiter().GetResult().Token == sample, "DPAPI decrypt"); }
            finally { Environment.SetEnvironmentVariable("KIMI_API_KEY", env); }
            vault.Save(p, ""); Check(!vault.HasSaved(p), "Forget credentials");
            HttpTests(root).GetAwaiter().GetResult();
            Check(Providers.All.All(x => new Uri(x.Endpoint).Scheme == "https") && Providers.All.Select(x => x.Id).Distinct().Count() == Providers.All.Length, "Fixed HTTPS endpoints and unique identifiers");
            Console.WriteLine("OK: " + checks + " checks; parsers, HTTP, retries, alerts, settings and encrypted credentials. No live provider requests.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAILED: " + ex.Message); return 1; }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); } // Only the unique test directory created by this run.
    }

    private static async Task HttpTests(string root)
    {
        var handler = new Handler { Respond = request => {
            Check(request.RequestUri.Host == "api.anthropic.com", "Auth sent only to expected host");
            Check(request.Headers.Authorization.Parameter == "test-credential" && request.Headers.Contains("anthropic-beta"), "Claude headers");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"five_hour\":{\"utilization\":5}}") };
        } };
        using (var client = new HttpClient(handler))
        {
            var data = await Usage.Fetch(Providers.Find("claude"), client, new Credentials(root), CancellationToken.None);
            Check(Value(data, "session") == 95, "HTTP response parsing");
            foreach (int code in new[] { 401, 403, 429, 503, 500, 302 })
            {
                handler.Respond = request => {
                    var response = new HttpResponseMessage((HttpStatusCode)code) { Content = new StringContent("test-private-response") };
                    if (code == 429) response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromHours(2));
                    return response;
                };
                bool caught = false;
                try { await Usage.Fetch(Providers.Find("claude"), client, new Credentials(root), CancellationToken.None); }
                catch (QuotaException ex) {
                    caught = true; Check(!ex.Message.Contains("test-private-response"), "HTTP body redacted");
                    if (code == 429) Check(ex.Retry == TimeSpan.FromHours(2), "HTTP Retry-After parsed");
                    if (code == 401) Check(ex.Kind == "auth", "401 authentication");
                    if (code == 403) Check(ex.Kind == "permission", "403 not blindly expired token");
                }
                Check(caught, "Reject HTTP " + code);
            }
            handler.Respond = request => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new string('x', 1048577)) };
            bool oversized = false;
            try { await Usage.Fetch(Providers.Find("claude"), client, new Credentials(root), CancellationToken.None); } catch (QuotaException) { oversized = true; }
            Check(oversized, "Bound server response size");
        }
    }
}
