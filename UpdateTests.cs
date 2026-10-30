using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UsageTray;

internal static class UpdateTests
{
    private sealed class Handler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
        { cancel.ThrowIfCancellationRequested(); return Task.FromResult(Respond(request)); }
    }
    private static string Release(string version)
    {
        return Json.Serialize(new { tag_name = "v" + version, draft = false, prerelease = false, assets = new[] { new {
            name = "QuotaDock-" + version + "-Setup.exe", size = 3,
            browser_download_url = "https://github.com/warante/quotadock/releases/download/v" + version + "/QuotaDock-" + version + "-Setup.exe",
            digest = "sha256:" + new string('a', 64) } } });
    }
    private static void Reject(Action action, Action<bool, string> check, string label)
    {
        bool rejected = false;
        try { action(); } catch (InvalidDataException) { rejected = true; }
        check(rejected, label);
    }
    public static void Run(Action<bool, string> check, string root)
    {
        var current = new Version("1.3.0");
        var text = Release("1.4.0");
        var update = Updates.Parse(text, current);
        check(update.Version == new Version("1.4.0"), "Detect newer stable release");
        check(Updates.Parse(text, new Version("1.4.0")) == null && Updates.Parse(text, new Version("1.5.0")) == null, "Never reinstall same or downgrade");
        check(Updates.Parse(Release("1.10.0"), new Version("1.9.0")) != null, "Compare versions numerically");
        Reject(() => Updates.Parse(text.Replace("\"draft\":false", "\"draft\":true"), current), check, "Reject drafts");
        Reject(() => Updates.Parse(text.Replace("\"prerelease\":false", "\"prerelease\":true"), current), check, "Reject prereleases");
        Reject(() => Updates.Parse(text.Replace("v1.4.0", "v1.4.0-beta"), current), check, "Reject unstable tag");
        Reject(() => Updates.Parse(text.Replace("warante/quotadock", "other/project"), current), check, "Reject foreign project download");
        Reject(() => Updates.Parse(text.Replace("\"digest\"", "\"missing\""), current), check, "Require GitHub SHA-256 digest");
        Reject(() => Updates.Parse(text.Replace("\"size\":3", "\"size\":99999999"), current), check, "Bound installer size");
        Reject(() => Updates.Parse(text.Replace("Setup.exe", "Portable.zip"), current), check, "Require expected installer");
        foreach (string url in new[] { "http://github.com/file", "https://example.com/file", "https://github.com.evil.example/file", "https://github.com:444/file" })
            check(!Updates.AllowedDownload(new Uri(url)), "Reject unsafe download redirect");
        check(!Updates.AllowedDownload(new UriBuilder("https://github.com/file") { UserName = "test-user" }.Uri), "Reject credentials in download URL");
        check(Updates.AllowedDownload(new Uri("https://release-assets.githubusercontent.com/test")), "Allow GitHub release asset host");
        byte[] bytes = { 1, 2, 3 };
        string digest;
        using (var sha = SHA256.Create()) digest = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        var verified = new AppUpdate(update.Version, update.Download, digest, 3);
        check(Updates.VerifyBytes(bytes, verified), "Verify correct SHA-256");
        check(!Updates.VerifyBytes(new byte[] { 1, 2, 4 }, verified) && !Updates.VerifyBytes(new byte[] { 1, 2 }, verified), "Reject corrupt or truncated download");
        int requests = 0;
        using (var client = new HttpClient(new Handler { Respond = request => {
            requests++;
            check(request.RequestUri.AbsoluteUri == Updates.LatestApi && request.Headers.Authorization == null && !request.Headers.Contains("Cookie"), "Update check uses fixed GitHub endpoint without credentials");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
        } }))
            check(Updates.Check(client, current, CancellationToken.None).GetAwaiter().GetResult().Version == update.Version && requests == 1, "Check does not download or install");
        using (var client = new HttpClient(new Handler { Respond = request => new HttpResponseMessage(HttpStatusCode.Forbidden) }))
        {
            bool failed = false;
            try { Updates.Check(client, current, CancellationToken.None).GetAwaiter().GetResult(); } catch (HttpRequestException) { failed = true; }
            check(failed, "GitHub rate limits fail without retries or installation");
        }
        using (var client = new HttpClient(new Handler { Respond = request => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[1024 * 1024 + 1]) } }))
            Reject(() => Updates.Check(client, current, CancellationToken.None).GetAwaiter().GetResult(), check, "Bound release metadata");
        requests = 0;
        using (var client = new HttpClient(new Handler { Respond = request => {
            requests++;
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri("https://example.com/installer.exe");
            return response;
        } }))
        {
            Reject(() => Updates.Download(client, verified, Path.Combine(root, "redirect"), CancellationToken.None).GetAwaiter().GetResult(), check, "Reject untrusted redirect before request");
            check(requests == 1, "Never contact foreign redirect host");
        }
        string folder = Path.Combine(root, "corrupt");
        using (var client = new HttpClient(new Handler { Respond = request => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[] { 1, 2, 4 }) } }))
            Reject(() => Updates.Download(client, verified, folder, CancellationToken.None).GetAwaiter().GetResult(), check, "Do not accept corrupt installer");
        check(Directory.GetFiles(folder).Length == 0, "Remove failed download without touching installation");
        using (var client = new HttpClient(new Handler { Respond = request => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) } }))
            Reject(() => Updates.Download(client, verified, Path.Combine(root, "not-executable"), CancellationToken.None).GetAwaiter().GetResult(), check, "Matching hash alone is insufficient without product metadata");
        using (var client = new HttpClient(new Handler { Respond = request => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 }) } }))
            Reject(() => Updates.Download(client, verified, Path.Combine(root, "oversized"), CancellationToken.None).GetAwaiter().GetResult(), check, "Reject download larger than advertised");
        requests = 0;
        using (var client = new HttpClient(new Handler { Respond = request => {
            requests++;
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = verified.Download;
            return response;
        } }))
            Reject(() => Updates.Download(client, verified, Path.Combine(root, "loop"), CancellationToken.None).GetAwaiter().GetResult(), check, "Bound redirect loops");
        check(requests == 6, "Maximum six download attempts including redirects");
        using (var cancel = new CancellationTokenSource())
        using (var client = new HttpClient(new Handler { Respond = request => { throw new Exception("Canceled checks must not send requests"); } }))
        {
            cancel.Cancel(); bool canceled = false;
            try { Updates.Check(client, current, cancel.Token).GetAwaiter().GetResult(); } catch (OperationCanceledException) { canceled = true; }
            check(canceled, "Stop update network work when canceled");
        }
        // Our own test executable supplies synthetic product/version metadata.
        // It is downloaded through a mock handler and is NEVER executed here.
        byte[] fixture = File.ReadAllBytes(typeof(UpdateTests).Assembly.Location);
        using (var sha = SHA256.Create()) digest = BitConverter.ToString(sha.ComputeHash(fixture)).Replace("-", "").ToLowerInvariant();
        var valid = new AppUpdate(new Version(Updates.CurrentVersion), new Uri("https://github.com/warante/quotadock/releases/download/v" + Updates.CurrentVersion + "/QuotaDock-" + Updates.CurrentVersion + "-Setup.exe"), digest, fixture.Length);
        requests = 0;
        using (var client = new HttpClient(new Handler { Respond = request => {
            requests++;
            check(request.Headers.Authorization == null && !request.Headers.Contains("Cookie"), "Downloads never send provider credentials");
            if (requests == 1)
            {
                var response = new HttpResponseMessage(HttpStatusCode.Redirect);
                response.Headers.Location = new Uri("https://release-assets.githubusercontent.com/synthetic-fixture");
                return response;
            }
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(fixture) };
        } }))
        {
            string file = Updates.Download(client, valid, Path.Combine(root, "valid-download"), CancellationToken.None).GetAwaiter().GetResult();
            check(File.Exists(file) && Updates.VerifyBytes(File.ReadAllBytes(file), valid) && requests == 2, "Verify installer through allowed GitHub redirect without launching it");
            Reject(() => Updates.Download(client, new AppUpdate(new Version("99.0.0"), new Uri("https://github.com/warante/quotadock/releases/download/v99.0.0/QuotaDock-99.0.0-Setup.exe"), digest, fixture.Length), Path.Combine(root, "wrong-version"), CancellationToken.None).GetAwaiter().GetResult(), check, "Reject product version that does not match release metadata");
        }
    }
}
