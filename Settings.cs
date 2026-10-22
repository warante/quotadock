using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace UsageTray
{
    public sealed class Preferences
    {
        public string[] Enabled = { "openai" };
        public string[] Monthly = new string[0];
        public bool Alerts;
        public int Threshold = 10;
    }

    public sealed class SettingsStore
    {
        public readonly string DirectoryPath;
        public SettingsStore(string path = null) { DirectoryPath = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuotaDock"); }
        public Preferences Load()
        {
            string path = Path.Combine(DirectoryPath, "settings.json");
            if (!File.Exists(path)) return new Preferences();
            var value = Json.Parse(File.ReadAllText(path));
            return new Preferences {
                Enabled = Json.Items(Json.Get(value, "enabled")).Select(Json.Text).Where(id => Providers.Find(id) != null).Distinct().ToArray(),
                Monthly = Json.Items(Json.Get(value, "monthly")).Select(Json.Text).Where(id => Providers.Find(id) != null).Distinct().ToArray(),
                Alerts = object.Equals(Json.Get(value, "alerts"), true),
                Threshold = Math.Max(1, Math.Min(50, (int)(Json.Number(Json.Get(value, "threshold")) ?? 10)))
            };
        }
        public void Save(Preferences prefs)
        {
            AtomicWrite(Path.Combine(DirectoryPath, "settings.json"), Encoding.UTF8.GetBytes(Json.Serialize(new {
                enabled = prefs.Enabled, monthly = prefs.Monthly, alerts = prefs.Alerts, threshold = prefs.Threshold
            })));
        }
        public Dictionary<string, RetryPolicy> LoadSchedules()
        {
            var result = new Dictionary<string, RetryPolicy>();
            string path = Path.Combine(DirectoryPath, "cooldowns.json");
            if (!File.Exists(path)) return result;
            foreach (var pair in Json.Obj(Json.Parse(File.ReadAllText(path))))
            {
                DateTimeOffset next;
                if (Providers.Find(pair.Key) != null && DateTimeOffset.TryParse(Json.Text(pair.Value), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out next))
                    result[pair.Key] = new RetryPolicy { Next = next };
            }
            return result;
        }
        public void SaveSchedules(Dictionary<string, RetryPolicy> schedules)
        {
            var values = schedules.ToDictionary(p => p.Key, p => p.Value.Next.ToString("o", CultureInfo.InvariantCulture));
            AtomicWrite(Path.Combine(DirectoryPath, "cooldowns.json"), Encoding.UTF8.GetBytes(Json.Serialize(values)));
        }
        internal static void AtomicWrite(string path, byte[] bytes)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                File.WriteAllBytes(temp, bytes);
                if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
            } finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }

    public sealed class AuthData
    {
        public string Token, Account;
        public AuthData(string token, string account = null) { Token = token; Account = account; }
    }

    public class CredentialStore
    {
        private readonly string directory;
        public CredentialStore(string root) { directory = Path.Combine(root, "credentials"); }
        private string PathFor(ProviderDefinition p)
        {
            if (Providers.Find(p.Id) == null) throw new InvalidOperationException("Proveedor desconocido.");
            return Path.Combine(directory, p.Id + ".bin");
        }
        public void Save(ProviderDefinition p, string secret)
        {
            if (!p.AcceptsKey) throw new InvalidOperationException("Este proveedor utiliza su sesión oficial.");
            string path = PathFor(p);
            if (string.IsNullOrWhiteSpace(secret)) { if (File.Exists(path)) File.Delete(path); return; }
            if (secret.Length > 16384 || secret.Any(char.IsControl)) throw new InvalidOperationException("La credencial contiene caracteres no válidos.");
            SettingsStore.AtomicWrite(path, ProtectedData.Protect(Encoding.UTF8.GetBytes(secret.Trim()), null, DataProtectionScope.CurrentUser));
        }
        public bool HasSaved(ProviderDefinition p) { return File.Exists(PathFor(p)); }
        public virtual async Task<AuthData> Read(ProviderDefinition p)
        {
            foreach (string name in p.Environments)
            {
                string token = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrWhiteSpace(token)) return Validate(new AuthData(token.Trim()));
            }
            if (p.AcceptsKey && HasSaved(p)) return Validate(new AuthData(Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(PathFor(p)), null, DataProtectionScope.CurrentUser))));
            if (p.Kind == "codex" || p.Kind == "claude" || p.Kind == "gemini")
            {
                string path = OfficialPath(p.Kind);
                if (!File.Exists(path)) throw new QuotaException("setup", "Falta la sesión local. " + p.Help);
                string text;
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(file)) text = reader.ReadToEnd();
                var root = Json.Parse(text);
                var bucket = p.Kind == "codex" ? Json.Get(root, "tokens") : p.Kind == "claude" ? Json.Get(root, "claudeAiOauth") : root;
                return Validate(new AuthData(Json.Text(Json.Get(bucket, p.Kind == "claude" ? "accessToken" : "access_token")),
                    p.Kind == "codex" ? Json.Text(Json.Get(bucket, "account_id")) : null));
            }
            if (p.Kind == "copilot")
            {
                string cli = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GitHub CLI", "gh.exe");
                if (File.Exists(cli))
                {
                    using (var process = new Process { StartInfo = new ProcessStartInfo(cli, "auth token --hostname github.com") {
                        UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
                    } })
                    {
                        process.Start();
                        var output = process.StandardOutput.ReadToEndAsync();
                        var error = process.StandardError.ReadToEndAsync();
                        if (!await Task.Run(() => process.WaitForExit(5000))) { process.Kill(); throw new QuotaException("setup", "GitHub CLI no respondió a tiempo."); }
                        await error; // Consume, never print stderr (may contain account details).
                        if (process.ExitCode == 0) return Validate(new AuthData((await output).Trim()));
                    }
                }
            }
            throw new QuotaException("setup", "Configura la credencial del proveedor en el menú o mediante " + string.Join(" / ", p.Environments) + ".");
        }
        private static AuthData Validate(AuthData auth)
        {
            if (string.IsNullOrWhiteSpace(auth.Token) || auth.Token.Length > 16384 || auth.Token.Any(char.IsControl) || (auth.Account != null && (auth.Account.Length > 4096 || auth.Account.Any(char.IsControl))))
                throw new QuotaException("auth", "No hay una sesión o credencial válida. Vuelve a conectar tu cuenta.");
            return auth;
        }
        public static string OfficialPath(string kind)
        {
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (kind == "claude")
            {
                string config = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
                return Path.Combine(string.IsNullOrWhiteSpace(config) ? Path.Combine(profile, ".claude") : config, ".credentials.json");
            }
            if (kind == "gemini") return Path.Combine(profile, ".gemini", "oauth_creds.json");
            string home = Environment.GetEnvironmentVariable("CODEX_HOME");
            if (!string.IsNullOrWhiteSpace(home)) return Path.Combine(home, "auth.json");
            string standard = Path.Combine(profile, ".codex", "auth.json");
            return File.Exists(standard) ? standard : Path.Combine(profile, ".config", "codex", "auth.json");
        }
    }

    public static class AutoStart
    {
        private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public static bool Enabled
        {
            get { try { using (var key = Registry.CurrentUser.OpenSubKey(Key)) return key != null && key.GetValue("QuotaDock") != null; } catch { return false; } }
        }
        public static void Set(bool enabled, string executable)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(Key))
            {
                if (enabled) key.SetValue("QuotaDock", "\"" + Path.GetFullPath(executable) + "\"");
                else key.DeleteValue("QuotaDock", false);
            }
        }
    }
}
