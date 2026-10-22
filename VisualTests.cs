using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using UsageTray;

internal static class VisualTests
{
    [DllImport("user32.dll")] private static extern int GetGuiResources(IntPtr process, int flag);
    [STAThread]
    public static int Main()
    {
        string root = Path.Combine(Environment.GetEnvironmentVariable("OPENCODE_TEST_TEMP") ?? Path.GetTempPath(), "quotadock-visual-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            using (var popup = new BarsPopup())
            {
                popup.SetProviders(new[] { Providers.Find("openai"), Providers.Find("claude"), Providers.Find("kimi") });
                var original = new[] { new Meter("session", "5 h", 81), new Meter("weekly", "Semana", 9) };
                popup.UpdateData(original, Color.MediumSeaGreen, "openai");
                bool switched = false;
                popup.ProviderSelected += id => {
                    switched = id == "claude";
                    popup.UpdateData(new[] { new Meter("session", "5 h", 7), new Meter("weekly", "Semana", 58) }, Providers.Find(id).Accent, id);
                };
                popup.Show(); Application.DoEvents();
                var buttons = popup.Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<Button>().ToArray();
                if (buttons.Length != 3) throw new Exception("Expected three provider tabs");
                buttons.Single(b => (string)b.Tag == "claude").PerformClick();
                if (!switched || popup.ProviderId != "claude" || !popup.Visible || popup.Data[0].Remaining != 7) throw new Exception("Tab must change both bars without closing popup");
                popup.UpdateData(original, Color.MediumSeaGreen, "openai");
                using (var bitmap = new Bitmap(popup.ClientSize.Width, popup.ClientSize.Height))
                {
                    popup.DrawToBitmap(bitmap, new Rectangle(Point.Empty, popup.ClientSize));
                    if (bitmap.GetPixel(124, popup.BarsTop + 9).G <= bitmap.GetPixel(124, popup.BarsTop + 9).R) throw new Exception("First bar not green");
                    if (bitmap.GetPixel(124, popup.BarsTop + 43).R <= bitmap.GetPixel(124, popup.BarsTop + 43).G) throw new Exception("Second bar not red");
                    bitmap.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preview.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                SavePreviews(popup);
                for (int i = 0; i < 5; i++) using (var icon = Visuals.Icon(Providers.All[0], popup.Data)) { }
                int before = GetGuiResources(Process.GetCurrentProcess().Handle, 0);
                for (int i = 0; i < 200; i++) using (var icon = Visuals.Icon(Providers.All[i % Providers.All.Length], popup.Data)) { }
                using (var buffer = new Bitmap(popup.Width, popup.Height))
                    for (int i = 0; i < 100; i++) { popup.UpdateData(original, Providers.All[i % 3].Accent, Providers.All[i % 3].Id); popup.DrawToBitmap(buffer, new Rectangle(Point.Empty, buffer.Size)); }
                GC.Collect(); GC.WaitForPendingFinalizers();
                int after = GetGuiResources(Process.GetCurrentProcess().Handle, 0);
                if (after > before + 10) throw new Exception("GDI resource leak");
                popup.SetProviders(Providers.All); Application.DoEvents();
                var panel = popup.Controls.OfType<FlowLayoutPanel>().Single();
                if (panel.Controls.Cast<Control>().Any(b => b.Bottom > panel.ClientSize.Height)) throw new Exception("Provider tabs are clipped");
                popup.Hide();
            }
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            if (typeof(TrayApp).GetFields(flags).Count(f => f.FieldType == typeof(NotifyIcon)) != 1 || typeof(ProviderState).GetFields(flags).Any(f => f.FieldType == typeof(NotifyIcon)))
                throw new Exception("Application must own exactly one tray icon, not one per provider");
            var store = new SettingsStore(root);
            store.Save(new Preferences { Enabled = new[] { "openai", "claude", "kimi" }, SelectedProvider = "openai" });
            int requests = 0;
            using (var app = new TrayApp(store, (p, token) => {
                requests++;
                return Task.FromResult(Usage.Parse("claude", "{\"five_hour\":{\"utilization\":10},\"seven_day\":{\"utilization\":20}}"));
            }, false))
            {
                app.SelectProvider("claude");
                if (requests != 0 || store.Load().SelectedProvider != "claude" || !app.TrayText.Contains("Claude")) throw new Exception("Selection must persist without making an API request");
                app.RefreshProvider("kimi").GetAwaiter().GetResult();
                if (requests != 1 || !app.TrayText.Contains("Claude") || app.SelectedProviderId != "claude") throw new Exception("Background refresh changed selected provider");
                app.RefreshProvider("claude").GetAwaiter().GetResult();
                if (!app.TrayText.Contains("90%")) throw new Exception("Selected provider refresh not displayed");
                app.SelectProvider("kimi");
                if (requests != 2 || !app.TrayText.Contains("90%") || !app.TrayText.Contains("Kimi")) throw new Exception("Selection must use existing provider cache");
                app.SelectProvider("invalid");
                if (app.SelectedProviderId != "kimi") throw new Exception("Disabled provider selected");
                app.ExitThread();
            }
            Console.WriteLine("OK: one tray icon, provider tabs, persisted selection, background isolation, cached switching, layout and GDI resources."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static void SavePreviews(BarsPopup popup)
    {
        var provider = Providers.Find("claude");
        popup.UpdateData(new[] { new Meter("session", "5 h", 7), new Meter("weekly", "Semana", 58) }, provider.Accent, provider.Id);
        using (var panel = new Bitmap(popup.ClientSize.Width, popup.ClientSize.Height))
        {
            popup.DrawToBitmap(panel, new Rectangle(Point.Empty, popup.ClientSize));
            // DrawToBitmap does not apply the native window region to its background.
            // Preserve the real rounded outline in the documentation's transparent PNG.
            if (popup.Region != null)
                for (int y = 0; y < panel.Height; y++)
                    for (int x = 0; x < panel.Width; x++)
                        if (!popup.Region.IsVisible(x, y)) panel.SetPixel(x, y, Color.Transparent);
            panel.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "provider-selector.png"), System.Drawing.Imaging.ImageFormat.Png);
            using (var hero = new Bitmap(960, 420))
            using (var g = Graphics.FromImage(hero))
            using (var title = new Font("Segoe UI", 34, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var body = new Font("Segoe UI", 18, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var small = new Font("Segoe UI", 14, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var white = new SolidBrush(Color.FromArgb(238, 242, 249)))
            using (var muted = new SolidBrush(Color.FromArgb(157, 172, 194)))
            using (var taskbar = new SolidBrush(Color.FromArgb(32, 36, 43)))
            using (var icon = Visuals.Icon(provider, popup.Data))
            {
                g.Clear(Color.FromArgb(16, 20, 28));
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.DrawString("QuotaDock", title, white, 38, 35);
                g.DrawString("Tu cuota, sin perder el foco.", body, muted, 40, 85);
                g.DrawString("Un solo icono.", body, white, 40, 196);
                g.DrawString("Cambia de proveedor al pulsar.\nLos demás siguen actualizándose.", body, muted, 40, 232);
                g.DrawImageUnscaled(panel, 500, 190);
                g.DrawString("Interfaz real · datos de demostración", small, muted, 40, 333);
                g.FillRectangle(taskbar, 0, 370, 960, 50);
                g.DrawString("^", body, muted, 710, 386);
                g.DrawIcon(icon, new Rectangle(754, 382, 28, 28));
                g.DrawString("12:34", body, white, 856, 383);
                hero.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "quotadock-preview.png"), System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
