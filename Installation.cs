using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32;

namespace UsageTray
{
    public enum LaunchOutcome { Started, BlockedByPolicy, Failed }

    public static class InstalledAppLauncher
    {
        public static LaunchOutcome TryStart(string executable, Action<string> launch)
        {
            try { launch(executable); return LaunchOutcome.Started; }
            catch (Win32Exception ex)
            {
                // Windows policy / signature enforcement errors, not an installation failure.
                return ex.NativeErrorCode == 1260 || ex.NativeErrorCode == 577 || ex.NativeErrorCode == 4551 ? LaunchOutcome.BlockedByPolicy : LaunchOutcome.Failed;
            }
            catch (Exception) { return LaunchOutcome.Failed; }
        }
    }

    public interface IInstallPlatform
    {
        byte[] Read(string path);
        void Write(string path, byte[] bytes);
        void Delete(string path);
        void Shortcut(string path, string target, string arguments);
        string Startup { get; set; }
        Dictionary<string, object> Registration { get; set; }
    }

    public sealed class InstallEngine
    {
        private readonly IInstallPlatform platform;
        public InstallEngine(IInstallPlatform platform) { this.platform = platform; }
        public void Install(string root, string programs, bool startup, string powershell, Func<string, byte[]> resource)
        {
            string target = Path.Combine(root, "QuotaDock.exe");
            string uninstall = Path.Combine(root, "Uninstall.ps1");
            string[] files = { target, uninstall, Path.Combine(programs, "QuotaDock.lnk"), Path.Combine(programs, "Desinstalar QuotaDock.lnk") };
            var previous = new Dictionary<string, byte[]>();
            foreach (string path in files) previous[path] = platform.Read(path);
            string oldStartup = platform.Startup;
            var oldRegistration = platform.Registration;
            try
            {
                // Load all resources before changing the machine.
                byte[] app = resource("QuotaDock.exe"); byte[] script = resource("Uninstall.ps1");
                if (app == null || script == null) throw new InvalidOperationException("Faltan recursos del instalador.");
                platform.Write(target, app); platform.Write(uninstall, script);
                platform.Shortcut(files[2], target, "");
                string arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + uninstall + "\"";
                platform.Shortcut(files[3], powershell, arguments);
                string command = "\"" + target + "\"";
                if (startup) platform.Startup = command;
                else if (platform.Startup == command) platform.Startup = null;
                platform.Registration = new Dictionary<string, object> {
                    { "DisplayName", "QuotaDock" }, { "DisplayVersion", "1.1.0" }, { "Publisher", "QuotaDock" },
                    { "InstallLocation", root }, { "DisplayIcon", target },
                    { "UninstallString", "\"" + powershell + "\" " + arguments }, { "NoModify", 1 }, { "NoRepair", 1 }
                };
            }
            catch
            {
                bool failed = false;
                foreach (var item in previous)
                {
                    try { if (item.Value == null) platform.Delete(item.Key); else platform.Write(item.Key, item.Value); }
                    catch { failed = true; }
                }
                try { platform.Startup = oldStartup; platform.Registration = oldRegistration; } catch { failed = true; }
                if (failed) throw new InvalidOperationException("La instalación falló y no se pudo restaurar todo. Cierra QuotaDock y vuelve a ejecutar el instalador.");
                throw;
            }
        }
    }

    public sealed class WindowsInstallPlatform : IInstallPlatform
    {
        private const string Run = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string Uninstall = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\QuotaDock";
        public byte[] Read(string path) { return File.Exists(path) ? File.ReadAllBytes(path) : null; }
        public void Write(string path, byte[] bytes) { SettingsStore.AtomicWrite(path, bytes); }
        public void Delete(string path) { if (File.Exists(path)) File.Delete(path); }
        public void Shortcut(string path, string target, string arguments)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            dynamic shortcut = shell.CreateShortcut(path);
            shortcut.TargetPath = target; shortcut.Arguments = arguments;
            shortcut.WorkingDirectory = Path.GetDirectoryName(target); shortcut.Description = "QuotaDock"; shortcut.Save();
        }
        public string Startup
        {
            get { using (var key = Registry.CurrentUser.OpenSubKey(Run)) return key == null ? null : key.GetValue("QuotaDock") as string; }
            set { using (var key = Registry.CurrentUser.CreateSubKey(Run)) { if (value == null) key.DeleteValue("QuotaDock", false); else key.SetValue("QuotaDock", value); } }
        }
        public Dictionary<string, object> Registration
        {
            get
            {
                using (var key = Registry.CurrentUser.OpenSubKey(Uninstall))
                {
                    if (key == null) return null;
                    var values = new Dictionary<string, object>();
                    foreach (string name in key.GetValueNames()) values[name] = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    return values;
                }
            }
            set
            {
                if (value == null) { Registry.CurrentUser.DeleteSubKey(Uninstall, false); return; }
                using (var key = Registry.CurrentUser.CreateSubKey(Uninstall))
                {
                    foreach (string name in key.GetValueNames()) if (!value.ContainsKey(name)) key.DeleteValue(name, false);
                    foreach (var pair in value) key.SetValue(pair.Key, pair.Value);
                }
            }
        }
    }
}
