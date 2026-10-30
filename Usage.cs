using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace UsageTray
{
    public sealed class QuotaException : Exception
    {
        public readonly string Kind;
        public readonly TimeSpan? Retry;
        public QuotaException(string kind, string message, TimeSpan? retry = null) : base(message) { Kind = kind; Retry = retry; }
    }

    public static class Json
    {
        public static Dictionary<string, object> Obj(object value) { return value as Dictionary<string, object> ?? new Dictionary<string, object>(); }
        public static object Get(object value, string key) { object item; return Obj(value).TryGetValue(key, out item) ? item : null; }
        public static string Text(object value) { return value as string ?? ""; }
        public static IEnumerable<object> Items(object value) { return value is string || value is IDictionary ? new object[0] : (value as IEnumerable ?? new object[0]).Cast<object>(); }
        public static double? Number(object value)
        {
            if (value == null || value is bool) return null;
            double n;
            return double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out n) && !double.IsNaN(n) && !double.IsInfinity(n) ? (double?)n : null;
        }
        public static object Parse(string text) { return new JavaScriptSerializer { MaxJsonLength = 1048576 }.DeserializeObject(text); }
        public static string Serialize(object value) { return new JavaScriptSerializer().Serialize(value); }
        public static double? Percent(object value, bool used)
        {
            var n = Number(value);
            return n.HasValue ? (double?)Math.Max(0, Math.Min(100, used ? 100 - n.Value : n.Value)) : null;
        }
    }

    public static class Usage
    {
        public static Snapshot Parse(string kind, string text, DateTimeOffset? receivedAt = null)
        {
            var observed = receivedAt ?? DateTimeOffset.UtcNow;
            object root = Json.Parse(text);
            if (!(root is Dictionary<string, object>) || Json.Get(root, "error") != null) throw new QuotaException("schema", "El proveedor devolvió un error o un formato no reconocido.");
            var result = new Snapshot();
            if (kind == "codex")
            {
                var limits = Json.Get(root, "rate_limit");
                string[] keys = { "primary_window", "secondary_window" };
                for (int i = 0; i < keys.Length; i++)
                {
                    var win = Json.Get(limits, keys[i]);
                    var duration = Json.Number(Json.Get(win, "limit_window_seconds"));
                    string id = duration == 18000 ? "session" : duration == 604800 ? "weekly" :
                        !duration.HasValue ? (i == 0 ? "session" : "weekly") : null;
                    var reset = Json.Get(win, "reset_at") != null ? ResetInfo.Unix(Json.Get(win, "reset_at"), false) : ResetInfo.After(Json.Get(win, "reset_after_seconds"), observed);
                    if (id != null && win != null) result.Add(id, id == "session" ? "5 h" : "Semana", Json.Percent(Json.Get(win, "used_percent"), true), reset);
                }
                result.Meters.Sort((a, b) => (a.Id == "session" ? 0 : 1).CompareTo(b.Id == "session" ? 0 : 1));
            }
            else if (kind == "claude")
            {
                result.Add("session", "5 h", Json.Percent(Json.Get(Json.Get(root, "five_hour"), "utilization"), true), ResetInfo.Iso(Json.Get(Json.Get(root, "five_hour"), "resets_at")));
                result.Add("weekly", "Semana", Json.Percent(Json.Get(Json.Get(root, "seven_day"), "utilization"), true), ResetInfo.Iso(Json.Get(Json.Get(root, "seven_day"), "resets_at")));
            }
            else if (kind == "go")
            {
                var usage = Json.Get(root, "usage");
                foreach (var key in new[] { "rolling", "weekly", "monthly" })
                {
                    var bucket = Json.Get(usage, key);
                    if (bucket == null) continue;
                    var status = Json.Text(Json.Get(bucket, "status"));
                    var percent = Json.Number(Json.Get(bucket, "percent"));
                    if ((status != "ok" && status != "rate-limited") || !percent.HasValue || percent < 0 || percent > 100) throw Schema();
                    result.Add(key == "rolling" ? "session" : key, key == "rolling" ? "5 h" : key == "weekly" ? "Semana" : "Mes", 100 - percent, ResetInfo.Iso(Json.Get(bucket, "resetsAt")));
                }
            }
            else if (kind == "kimi")
            {
                foreach (var limit in Json.Items(Json.Get(root, "limits")))
                {
                    var window = Json.Get(limit, "window");
                    string unit = Json.Text(Json.Get(window, "timeUnit") ?? Json.Get(window, "time_unit")).Replace("TIME_UNIT_", "");
                    var duration = Json.Number(Json.Get(window, "duration"));
                    if ((duration == 300 && (unit == "MINUTE" || unit == "MINUTES")) || (duration == 5 && (unit == "HOUR" || unit == "HOURS")))
                    { result.Add("session", "5 h", Counter(Json.Get(limit, "detail")), ResetInfo.Kimi(Json.Get(limit, "detail"))); break; }
                }
                if (!result.Meters.Any()) result.Add("session", "5 h", null);
                var weekly = Json.Get(root, "usage");
                if (weekly != null) result.Add("weekly", "Semana", Counter(weekly), ResetInfo.Kimi(weekly));
                else
                {
                    var ratio = Json.Number(Json.Get(Json.Get(Json.Get(root, "usages"), "limit_month_total"), "used_ratio"));
                    if (ratio.HasValue && ratio >= 0 && ratio <= 1) result.Add("monthly", "Mes", 100 * (1 - ratio), ResetInfo.Kimi(Json.Get(Json.Get(root, "usages"), "limit_month_total")));
                }
            }
            else if (kind == "minimax")
            {
                var code = Json.Number(Json.Get(Json.Get(root, "base_resp"), "status_code"));
                if (code == 1004 || code == 2049) throw new QuotaException("auth", "MiniMax ha rechazado la clave de suscripción o su región.");
                if (code != 0) throw new QuotaException("service", "MiniMax devolvió un error de servicio.");
                var row = Json.Items(Json.Get(root, "model_remains")).FirstOrDefault(r => Json.Text(Json.Get(r, "model_name")) == "general");
                if (row == null) throw Schema(); // Do not silently substitute a video/model pool.
                var start = Json.Number(Json.Get(row, "start_time"));
                var end = Json.Number(Json.Get(row, "end_time"));
                string label = start.HasValue && end.HasValue && end > start && end - start != 18000000 ? "Intervalo" : "5 h";
                result.Add("session", label, Json.Percent(Json.Get(row, "current_interval_remaining_percent"), false), ResetInfo.Unix(Json.Get(row, "end_time"), true));
                result.Add("weekly", "Semana", Json.Percent(Json.Get(row, "current_weekly_remaining_percent"), false), ResetInfo.Unix(Json.Get(row, "weekly_end_time"), true));
            }
            else if (kind == "zai")
            {
                var code = Json.Number(Json.Get(root, "code"));
                if (code == 1000 || code == 401 || code == 403) throw new QuotaException("auth", "El proveedor ha rechazado la clave del Coding Plan.");
                if (!object.Equals(Json.Get(root, "success"), true) || (code.HasValue && code != 200 && code != 0)) throw new QuotaException("service", "El proveedor devolvió un error de servicio.");
                foreach (var id in new[] { "session", "weekly", "monthly" })
                {
                    var matches = Json.Items(Json.Get(Json.Get(root, "data"), "limits")).Where(row =>
                    {
                        string type = Json.Text(Json.Get(row, "type"));
                        if (type != "TOKENS_LIMIT" && type != "CREDIT_LIMIT") return false;
                        var unit = Json.Number(Json.Get(row, "unit")); var number = Json.Number(Json.Get(row, "number"));
                        return id == "session" ? unit == 3 && (!number.HasValue || number == 5) : id == "weekly" ? unit == 6 && (!number.HasValue || number == 1) : unit == 5 && number == 1;
                    }).ToArray();
                    if (matches.Length > 1) throw Schema();
                    if (matches.Length == 1) result.Add(id, id == "session" ? "5 h" : id == "weekly" ? "Semana" : "Mes", Json.Percent(Json.Get(matches[0], "percentage"), true), ResetInfo.Unix(Json.Get(matches[0], "nextResetTime"), true));
                }
            }
            else if (kind == "copilot")
            {
                var quotas = Json.Get(root, "quota_snapshots");
                var reset = ResetInfo.Iso(Json.Get(root, "quota_reset_date_utc") ?? Json.Get(root, "quota_reset_date"));
                result.Add("premium", "Premium/mes", CopilotQuota(Json.Get(quotas, "premium_interactions")), reset);
                result.Add("chat", "Chat/mes", CopilotQuota(Json.Get(quotas, "chat")), reset);
            }
            else if (kind == "gemini")
            {
                foreach (string family in new[] { "pro", "flash" })
                {
                    var buckets = Json.Items(Json.Get(root, "buckets")).Where(b =>
                    {
                        var model = Json.Text(Json.Get(b, "modelId")).ToLowerInvariant();
                        return model.Contains(family) && !model.Contains("lite");
                    }).Where(b => { var n = Json.Number(Json.Get(b, "remainingFraction")); return n.HasValue && n >= 0 && n <= 1; }).ToArray();
                    var minimum = buckets.Any() ? buckets.Min(b => Json.Number(Json.Get(b, "remainingFraction"))) : null;
                    var resets = buckets.Where(b => Json.Number(Json.Get(b, "remainingFraction")) == minimum).Select(b => ResetInfo.Iso(Json.Get(b, "resetTime"))).ToArray();
                    ResetInfo reset = resets.Length > 0 && resets.All(r => r != null && r.At.HasValue && r.At == resets[0].At) ? resets[0] : null;
                    result.Add(family, family == "pro" ? "Pro" : "Flash", 100 * minimum, reset);
                }
            }
            if (!result.Meters.Any(m => m.Remaining.HasValue)) throw Schema();
            return result;
        }

        private static double? Counter(object bucket)
        {
            var limit = Json.Number(Json.Get(bucket, "limit"));
            if (!limit.HasValue || limit <= 0) return null;
            var remaining = Json.Number(Json.Get(bucket, "remaining"));
            var used = Json.Number(Json.Get(bucket, "used"));
            if (remaining.HasValue && remaining >= 0) return Json.Percent(100 * remaining / limit, false);
            if (used.HasValue && used >= 0) return Json.Percent(100 * used / limit, true);
            return null;
        }
        private static double? CopilotQuota(object quota)
        {
            // Unlimited is not a finite 100% quota. Keep it unknown instead of inventing a limit.
            if (object.Equals(Json.Get(quota, "unlimited"), true)) return null;
            return Json.Percent(Json.Get(quota, "percent_remaining"), false) ??
                Counter(new Dictionary<string, object> { { "limit", Json.Get(quota, "entitlement") }, { "remaining", Json.Get(quota, "remaining") } });
        }
        private static QuotaException Schema() { return new QuotaException("schema", "No hay cuotas reconocibles: formato cambiado, límite ausente o plan no compatible."); }

        public static async Task<Snapshot> Fetch(ProviderDefinition provider, HttpClient client, CredentialStore credentials, CancellationToken cancellation)
        {
            AuthData auth = await credentials.Read(provider);
            string project = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
            if (provider.Kind == "gemini" && string.IsNullOrWhiteSpace(project))
            {
                const string loadUrl = "https://cloudcode-pa.googleapis.com/v1internal:loadCodeAssist";
                string load = await Request(provider, client, auth, loadUrl, "{\"metadata\":{\"ideType\":\"IDE_UNSPECIFIED\",\"platform\":\"PLATFORM_UNSPECIFIED\",\"pluginType\":\"GEMINI\"},\"mode\":\"HEALTH_CHECK\"}", cancellation);
                project = Json.Text(Json.Get(Json.Parse(load), "cloudaicompanionProject"));
                if (string.IsNullOrWhiteSpace(project)) throw new QuotaException("setup", "Google no comunica el proyecto. Define GOOGLE_CLOUD_PROJECT y reinicia QuotaDock.");
            }
            string body = provider.Kind == "gemini" ? Json.Serialize(new Dictionary<string, string> { { "project", project } }) : null;
            var text = await Request(provider, client, auth, provider.Endpoint, body, cancellation);
            return Parse(provider.Kind, text);
        }

        private static async Task<string> Request(ProviderDefinition provider, HttpClient client, AuthData auth, string endpoint, string body, CancellationToken cancellation)
        {
            // Test handlers may intercept this request, but production URLs are never configurable.
            if (endpoint != provider.Endpoint && !(provider.Kind == "gemini" && endpoint == "https://cloudcode-pa.googleapis.com/v1internal:loadCodeAssist")) throw new InvalidOperationException("Destino no permitido.");
            using (var request = new HttpRequestMessage(body == null ? HttpMethod.Get : HttpMethod.Post, endpoint))
            {
                request.Headers.TryAddWithoutValidation("Authorization", provider.Kind == "zai" ? auth.Token : (provider.Kind == "copilot" ? "token " : "Bearer ") + auth.Token);
                request.Headers.UserAgent.ParseAdd("QuotaDock/1.4.0");
                request.Headers.Accept.ParseAdd("application/json");
                if (provider.Kind == "claude") request.Headers.Add("anthropic-beta", "oauth-2025-04-20");
                if (provider.Kind == "codex" && !string.IsNullOrWhiteSpace(auth.Account)) request.Headers.Add("ChatGPT-Account-Id", auth.Account);
                if (provider.Kind == "copilot")
                {
                    request.Headers.Add("Editor-Version", "vscode/1.96.2");
                    request.Headers.Add("Editor-Plugin-Version", "copilot-chat/0.26.7");
                    request.Headers.Add("X-GitHub-Api-Version", "2025-04-01");
                }
                if (body != null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation))
                {
                    var status = (int)response.StatusCode;
                    if (status == 401) throw new QuotaException("auth", "Sesión caducada o credencial rechazada. Vuelve a conectar tu cuenta.");
                    if (status == 403) throw new QuotaException("permission", "Acceso denegado: comprueba permisos, suscripción y región. También puede ser un bloqueo del proveedor.");
                    if (status == 429 || status == 503)
                    {
                        var retry = response.Headers.RetryAfter;
                        TimeSpan? delay = retry == null ? null : retry.Delta ?? (retry.Date.HasValue ? (TimeSpan?)(retry.Date.Value - DateTimeOffset.UtcNow) : null);
                        throw new QuotaException("throttle", "Proveedor ocupado o límite de consultas; esperando para reintentar.", delay);
                    }
                    if (!response.IsSuccessStatusCode) throw new QuotaException("service", "Consulta rechazada (HTTP " + status + ").");
                    // Bound both memory and time. Never display or log raw server responses.
                    if (response.Content.Headers.ContentLength > 1048576) throw Schema();
                    using (var abort = cancellation.Register(() => { try { response.Dispose(); } catch { } }))
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var memory = new MemoryStream())
                    {
                        var buffer = new byte[8192]; int count;
                        while ((count = await stream.ReadAsync(buffer, 0, buffer.Length, cancellation)) > 0)
                        {
                            if (memory.Length + count > 1048576) throw Schema();
                            memory.Write(buffer, 0, count);
                        }
                        return Encoding.UTF8.GetString(memory.ToArray());
                    }
                }
            }
        }
    }

    public sealed class RetryPolicy
    {
        public DateTimeOffset Next = DateTimeOffset.MinValue;
        public int Failures;
        public void Success(DateTimeOffset now, int interval) { Failures = 0; Next = now.AddSeconds(interval); }
        public void Fail(DateTimeOffset now, int interval, QuotaException error)
        {
            Failures = Math.Min(10, Failures + 1);
            double seconds = Math.Min(3600, Math.Max(interval, 60) * Math.Pow(2, Failures));
            if (error != null && error.Retry.HasValue && error.Retry.Value.TotalSeconds > seconds) seconds = error.Retry.Value.TotalSeconds;
            // Honor long server cooldowns too; timer checks elapsed time rather than storing an int interval.
            Next = now.AddSeconds(Math.Min(seconds, TimeSpan.FromDays(365).TotalSeconds));
        }
        public bool CanFetch(DateTimeOffset now) { return now >= Next; }
    }

    public sealed class AlertGate
    {
        private readonly HashSet<string> low = new HashSet<string>();
        public bool Observe(Meter meter, int threshold)
        {
            if (!meter.Remaining.HasValue) return false;
            if (meter.Remaining > threshold + 2) { low.Remove(meter.Id); return false; }
            if (meter.Remaining <= threshold) return low.Add(meter.Id);
            return false;
        }
        public void Reset() { low.Clear(); }
    }
}
