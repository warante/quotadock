using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace UsageTray
{
    public sealed class AppUpdate
    {
        public readonly Version Version;
        public readonly Uri Download;
        public readonly string Sha256;
        public readonly long Size;
        public AppUpdate(Version version, Uri download, string sha256, long size) { Version = version; Download = download; Sha256 = sha256; Size = size; }
    }

    public static class Updates
    {
        public const string CurrentVersion = "1.4.0";
        public const string LatestApi = "https://api.github.com/repos/warante/quotadock/releases/latest";
        internal const int MaxInstallerBytes = 16 * 1024 * 1024;
        public static HttpClient CreateClient()
        {
            return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, UseDefaultCredentials = false }) { Timeout = TimeSpan.FromSeconds(90) };
        }
        public static AppUpdate Parse(string text, Version current)
        {
            var root = Json.Parse(text);
            Version version;
            string tag = Json.Text(Json.Get(root, "tag_name"));
            if (!object.Equals(Json.Get(root, "draft"), false) || !object.Equals(Json.Get(root, "prerelease"), false) ||
                !Regex.IsMatch(tag, @"^v(?:0|[1-9]\d*)\.(?:0|[1-9]\d*)\.(?:0|[1-9]\d*)$") || !Version.TryParse(tag.Substring(1), out version))
                throw new InvalidDataException("Invalid stable release.");
            if (version <= current) return null;
            string name = "QuotaDock-" + tag.Substring(1) + "-Setup.exe";
            var assets = Json.Items(Json.Get(root, "assets")).Where(a => Json.Text(Json.Get(a, "name")) == name).ToArray();
            if (assets.Length != 1) throw new InvalidDataException("Missing unique installer.");
            string url = Json.Text(Json.Get(assets[0], "browser_download_url"));
            string expected = "https://github.com/warante/quotadock/releases/download/" + tag + "/" + name;
            string digest = Json.Text(Json.Get(assets[0], "digest"));
            var size = Json.Number(Json.Get(assets[0], "size"));
            if (url != expected || !Regex.IsMatch(digest, @"^sha256:[a-fA-F0-9]{64}$") || !size.HasValue || size < 1 || size > MaxInstallerBytes || Math.Truncate(size.Value) != size)
                throw new InvalidDataException("Invalid installer metadata.");
            return new AppUpdate(version, new Uri(expected), digest.Substring(7).ToLowerInvariant(), (long)size.Value);
        }
        public static async Task<AppUpdate> Check(HttpClient client, Version current, CancellationToken cancel)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, LatestApi))
            {
                request.Headers.UserAgent.ParseAdd("QuotaDock/" + CurrentVersion);
                request.Headers.Accept.ParseAdd("application/vnd.github+json");
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel))
                {
                    response.EnsureSuccessStatusCode();
                    using (var memory = new MemoryStream())
                    { await CopyBounded(response.Content, memory, 1024 * 1024, cancel); return Parse(Encoding.UTF8.GetString(memory.ToArray()), current); }
                }
            }
        }
        internal static bool AllowedDownload(Uri uri)
        {
            return uri != null && uri.IsAbsoluteUri && uri.Scheme == "https" && uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo) &&
                (uri.Host == "github.com" || uri.Host == "release-assets.githubusercontent.com");
        }
        internal static bool VerifyBytes(byte[] bytes, AppUpdate update)
        {
            using (var hash = SHA256.Create()) return bytes.LongLength == update.Size && Hex(hash.ComputeHash(bytes)) == update.Sha256;
        }
        private static string Hex(byte[] bytes) { return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant(); }
        private static async Task CopyBounded(HttpContent content, Stream target, long limit, CancellationToken cancel)
        {
            if (content.Headers.ContentLength > limit) throw new InvalidDataException("Response too large.");
            using (var source = await content.ReadAsStreamAsync())
            {
                var buffer = new byte[16384]; long total = 0; int count;
                while ((count = await source.ReadAsync(buffer, 0, buffer.Length, cancel)) > 0)
                { total += count; if (total > limit) throw new InvalidDataException("Response too large."); await target.WriteAsync(buffer, 0, count, cancel); }
            }
        }
        public static async Task<string> Download(HttpClient client, AppUpdate update, string directory, CancellationToken cancel)
        {
            string expected = "https://github.com/warante/quotadock/releases/download/v" + update.Version + "/QuotaDock-" + update.Version + "-Setup.exe";
            if (update.Download.AbsoluteUri != expected || update.Size < 1 || update.Size > MaxInstallerBytes || !Regex.IsMatch(update.Sha256, "^[a-f0-9]{64}$")) throw new InvalidDataException("Invalid download.");
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, "QuotaDock-" + update.Version + "-Setup.exe");
            bool created = false;
            try
            {
                Uri uri = update.Download;
                for (int redirect = 0; redirect <= 5; redirect++)
                {
                    if (!AllowedDownload(uri)) throw new InvalidDataException("Untrusted download host.");
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                    {
                        request.Headers.UserAgent.ParseAdd("QuotaDock/" + CurrentVersion);
                        using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel))
                        {
                            int status = (int)response.StatusCode;
                            if (status >= 300 && status < 400)
                            {
                                var location = response.Headers.Location;
                                if (location == null) throw new InvalidDataException("Missing redirect target.");
                                uri = location.IsAbsoluteUri ? location : new Uri(uri, location); continue;
                            }
                            response.EnsureSuccessStatusCode();
                            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                            { created = true; await CopyBounded(response.Content, stream, update.Size, cancel); }
                            using (var stream = File.OpenRead(file))
                            using (var hash = SHA256.Create())
                                if (stream.Length != update.Size || Hex(hash.ComputeHash(stream)) != update.Sha256) throw new InvalidDataException("Installer digest mismatch.");
                            var info = FileVersionInfo.GetVersionInfo(file);
                            if (info.ProductName != "QuotaDock" || info.ProductVersion != update.Version.ToString() || info.FileVersion != update.Version + ".0") throw new InvalidDataException("Installer version mismatch.");
                            return file;
                        }
                    }
                }
                throw new InvalidDataException("Too many redirects.");
            }
            catch
            {
                if (created && File.Exists(file)) File.Delete(file);
                throw;
            }
        }
    }
}
