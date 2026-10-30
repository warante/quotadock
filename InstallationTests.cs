using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using UsageTray;

internal static class InstallationTests
{
    private sealed class Sandbox : IInstallPlatform
    {
        public readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
        public bool FailShortcut;
        public byte[] Read(string path) { byte[] value; return Files.TryGetValue(path, out value) ? value : null; }
        public void Write(string path, byte[] bytes) { Files[path] = bytes; }
        public void Delete(string path) { Files.Remove(path); }
        public void Shortcut(string path, string target, string arguments)
        {
            if (FailShortcut) throw new IOException("Synthetic install failure");
            Write(path, System.Text.Encoding.UTF8.GetBytes(target + arguments));
        }
        public string Startup { get; set; }
        public Dictionary<string, object> Registration { get; set; }
    }
    private static void Check(bool ok) { if (!ok) throw new Exception("Installation assertion failed"); }
    public static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--cooperative-exit")
        {
            using (var exit = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\QuotaDock.ExitForUpdate." + Process.GetCurrentProcess().Id))
            using (var ready = EventWaitHandle.OpenExisting(args[1]))
            { ready.Set(); return exit.WaitOne(15000) ? 0 : 1; }
        }
        try
        {
            string root = Path.Combine("sandbox", "Install"); string programs = Path.Combine("sandbox", "Programs");
            var platform = new Sandbox(); var engine = new InstallEngine(platform);
            Func<string, byte[]> resources = name => System.Text.Encoding.UTF8.GetBytes(name);
            platform.Files[Path.Combine(root, "unrelated.txt")] = new byte[] { 123 };
            engine.Install(root, programs, true, "powershell", resources);
            Check(platform.Files.Count == 7 && platform.Startup == "\"" + Path.Combine(root, "QuotaDock.exe") + "\"" && (string)platform.Registration["DisplayVersion"] == "1.4.0");
            engine.Install(root, programs, false, "powershell", resources); Check(platform.Files.Count == 7 && platform.Startup == null);
            var before = platform.Files.ToDictionary(x => x.Key, x => x.Value);
            platform.Startup = "previous-portable-app";
            platform.FailShortcut = true;
            bool failed = false;
            try { engine.Install(root, programs, true, "powershell", name => new byte[] { 42 }); } catch (IOException) { failed = true; }
            Check(failed && platform.Startup == "previous-portable-app" && platform.Files.All(x => x.Value.SequenceEqual(before[x.Key])));
            var clean = new Sandbox { FailShortcut = true };
            try { new InstallEngine(clean).Install(root, programs, true, "powershell", resources); } catch (IOException) { }
            Check(clean.Files.Count == 0 && clean.Startup == null && clean.Registration == null);
            Check(InstalledAppLauncher.TryStart("test-app", path => { }) == LaunchOutcome.Started);
            foreach (int code in new[] { 1260, 577, 4551 })
                Check(InstalledAppLauncher.TryStart("test-app", path => { throw new Win32Exception(code); }) == LaunchOutcome.BlockedByPolicy);
            Check(InstalledAppLauncher.TryStart("test-app", path => { throw new Win32Exception(2); }) == LaunchOutcome.Failed);
            Check(InstalledAppLauncher.TryStart("test-app", path => { throw new IOException("Synthetic launch failure"); }) == LaunchOutcome.Failed);
            Check(platform.Files.ContainsKey(Path.Combine(root, "QuotaDock.exe"))); // A launch failure never rolls back the installation.
            // Separate fixture process: no TrayApp, real settings, provider credentials or network.
            string fixtureRoot = Path.Combine(Environment.GetEnvironmentVariable("OPENCODE_TEST_TEMP") ?? Path.GetTempPath(), "quotadock-close-test-" + Guid.NewGuid().ToString("N"));
            string executable = Path.Combine(fixtureRoot, "QuotaDock.exe");
            Directory.CreateDirectory(fixtureRoot);
            try
            {
                File.Copy(typeof(InstallationTests).Assembly.Location, executable);
                string readyName = "Local\\QuotaDock.UpdateTestReady." + Guid.NewGuid().ToString("N");
                using (var ready = new EventWaitHandle(false, EventResetMode.AutoReset, readyName))
                using (var child = Process.Start(new ProcessStartInfo(executable, "--cooperative-exit " + readyName) { UseShellExecute = false, CreateNoWindow = true }))
                {
                    Check(ready.WaitOne(5000));
                    Check(InstalledInstance.RequestClose(Path.Combine(fixtureRoot, "other", "QuotaDock.exe")) && !child.HasExited);
                    Check(InstalledInstance.RequestClose(executable) && child.WaitForExit(5000) && child.ExitCode == 0);
                }
            }
            finally { File.Delete(executable); Directory.Delete(fixtureRoot); }
            Console.WriteLine("OK: install, update, rollback, preservation of unrelated files and launch/policy error handling (isolated platform).");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
    }
}
