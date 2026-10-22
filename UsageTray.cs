using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace UsageTray
{
    public sealed class Snapshot
    {
        public double? FiveHours;
        public double? Week;
    }

    public static class Usage
    {
        public static Dictionary<string, object> Object(object value)
        {
            return value as Dictionary<string, object> ?? new Dictionary<string, object>();
        }

        public static object Get(Dictionary<string, object> obj, string key)
        {
            object value;
            return obj.TryGetValue(key, out value) ? value : null;
        }

        public static Snapshot Parse(string json)
        {
            if (Provider.IsClaude) return ParseClaude(json);
            var root = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
            var limits = Object(Get(root, "rate_limit"));
            var result = new Snapshot();
            string[] keys = { "primary_window", "secondary_window" };
            for (int i = 0; i < keys.Length; i++)
            {
                var window = Object(Get(limits, keys[i]));
                double used;
                if (!double.TryParse(Convert.ToString(Get(window, "used_percent"), CultureInfo.InvariantCulture),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out used) || double.IsNaN(used) || double.IsInfinity(used)) continue;
                double remaining = Math.Max(0, Math.Min(100, 100 - used));
                long seconds;
                long.TryParse(Convert.ToString(Get(window, "limit_window_seconds")), out seconds);
                if (seconds == 18000 || (seconds == 0 && i == 0)) result.FiveHours = remaining;
                if (seconds == 604800 || (seconds == 0 && i == 1)) result.Week = remaining;
            }
            return result;
        }

        public static Snapshot ParseClaude(string json)
        {
            var root = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
            return new Snapshot {
                FiveHours = ClaudeRemaining(Get(root, "five_hour")),
                Week = ClaudeRemaining(Get(root, "seven_day"))
            };
        }

        private static double? ClaudeRemaining(object bucket)
        {
            double used;
            if (!double.TryParse(Convert.ToString(Get(Object(bucket), "utilization"), CultureInfo.InvariantCulture),
                NumberStyles.Float, CultureInfo.InvariantCulture, out used) || double.IsNaN(used) || double.IsInfinity(used)) return null;
            return Math.Max(0, Math.Min(100, 100 - used));
        }

        public static string AuthPath()
        {
            if (Provider.IsClaude)
            {
                string config = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
                return Path.Combine(string.IsNullOrWhiteSpace(config) ?
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude") : config,
                    ".credentials.json");
            }
            string home = Environment.GetEnvironmentVariable("CODEX_HOME");
            if (!string.IsNullOrWhiteSpace(home)) return Path.Combine(home, "auth.json");
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string standard = Path.Combine(profile, ".codex", "auth.json");
            return File.Exists(standard) ? standard : Path.Combine(profile, ".config", "codex", "auth.json");
        }

        // Read afresh each time: the official client owns and renews this file. Never alter its tokens.
        public static async Task<Snapshot> Fetch(HttpClient client)
        {
            string path = AuthPath();
            if (!File.Exists(path)) throw new InvalidOperationException("Inicia sesión en " + Provider.Login + ".");
            string json;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream)) json = reader.ReadToEnd();
            var root = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
            var tokens = Object(Get(root, Provider.IsClaude ? "claudeAiOauth" : "tokens"));
            string access = Get(tokens, Provider.IsClaude ? "accessToken" : "access_token") as string;
            if (string.IsNullOrWhiteSpace(access)) throw new InvalidOperationException("Inicia sesión en " + Provider.Login + ", no con una clave API.");
            using (var request = new HttpRequestMessage(HttpMethod.Get, Provider.Endpoint))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access);
                request.Headers.UserAgent.ParseAdd(Provider.AppName + "/1.0");
                if (Provider.IsClaude) request.Headers.Add("anthropic-beta", "oauth-2025-04-20");
                string account = Get(tokens, "account_id") as string;
                if (!Provider.IsClaude && !string.IsNullOrWhiteSpace(account)) request.Headers.Add("ChatGPT-Account-Id", account);
                using (var response = await client.SendAsync(request))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                        throw new InvalidOperationException("Sesión caducada o sin acceso: vuelve a iniciar sesión en " + Provider.Name + ".");
                    if ((int)response.StatusCode == 429) throw new InvalidOperationException(Provider.Service + " limita las consultas; reintentando en unos minutos.");
                    if (!response.IsSuccessStatusCode) throw new InvalidOperationException(Provider.Service + " no disponible (HTTP " + (int)response.StatusCode + ").");
                    return Parse(await response.Content.ReadAsStringAsync());
                }
            }
        }

        public static string Percent(double? value)
        {
            return value.HasValue ? Math.Round(value.Value).ToString(CultureInfo.InvariantCulture) + "%" : "—%";
        }

        public static Color BarColor(double? value)
        {
            return !value.HasValue ? Color.FromArgb(120, 126, 138) :
                value.Value <= 10 ? Color.FromArgb(246, 94, 94) :
                value.Value <= 25 ? Color.FromArgb(245, 185, 66) : Color.FromArgb(61, 211, 158);
        }
    }

    public sealed class BarsPopup : Form
    {
        public Snapshot Data = new Snapshot();
        public BarsPopup()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            ClientSize = new Size(280, 76);
            BackColor = Color.FromArgb(28, 30, 35);
            DoubleBuffered = true;
            Deactivate += delegate { Hide(); };
            KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Hide(); };
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var font = new Font("Segoe UI", 9))
            {
                DrawRow(e.Graphics, font, "5 h", Data.FiveHours, 13);
                DrawRow(e.Graphics, font, "Semana", Data.Week, 45);
            }
        }
        private static void DrawRow(Graphics g, Font font, string label, double? value, int y)
        {
            using (var text = new SolidBrush(Color.WhiteSmoke))
            using (var track = new SolidBrush(Color.FromArgb(62, 66, 76)))
            using (var fill = new SolidBrush(Usage.BarColor(value)))
            {
                g.DrawString(label, font, text, 12, y - 2);
                g.FillRectangle(track, 70, y + 2, 145, 12);
                if (value.HasValue) g.FillRectangle(fill, 70, y + 2, (float)(145 * value.Value / 100), 12);
                else g.DrawString("—", font, text, 132, y - 2);
                g.DrawString(Usage.Percent(value), font, text, 225, y - 2);
            }
        }
        public void Toggle()
        {
            if (Visible) { Hide(); return; }
            Rectangle work = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(Math.Max(work.Left, Math.Min(Cursor.Position.X - Width / 2, work.Right - Width)),
                Math.Max(work.Top, Math.Min(Cursor.Position.Y - Height - 12, work.Bottom - Height - 8)));
            Show();
            Activate();
        }
    }

    public sealed class TrayApp : ApplicationContext
    {
        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);
        private readonly NotifyIcon tray = new NotifyIcon();
        private readonly BarsPopup popup = new BarsPopup();
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private readonly HttpClient client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        private readonly ToolStripMenuItem status = new ToolStripMenuItem("Conectando…");
        private bool busy;
        private bool stopped;
        private int failures;
        private Icon currentIcon;

        public TrayApp()
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            var menu = new ContextMenuStrip();
            status.Enabled = false;
            menu.Items.Add(status);
            menu.Items.Add("Actualizar", null, async delegate { await Refresh(); });
            menu.Items.Add("Cómo conectar", null, delegate {
                MessageBox.Show("Usa una sesión de " + Provider.Login + ".\n\n" +
                    "Se lee: " + Usage.AuthPath() + "\n\n" +
                    "Si caduca, vuelve a iniciar sesión en " + Provider.Name + ". No se guardan ni se modifican tus credenciales.",
                    "Conectar " + Provider.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            menu.Items.Add("Salir", null, delegate { ExitThread(); });
            tray.ContextMenuStrip = menu;
            tray.MouseClick += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) popup.Toggle(); };
            Paint(new Snapshot());
            tray.Visible = true;
            timer.Interval = 60000;
            timer.Tick += async delegate { await Refresh(); };
            timer.Start();
            // Schedule once the WinForms message loop has installed its UI context.
            EventHandler start = null;
            start = async delegate { Application.Idle -= start; await Refresh(); };
            Application.Idle += start;
        }

        private async Task Refresh()
        {
            if (busy || stopped) return;
            busy = true;
            try
            {
                Snapshot data = await Usage.Fetch(client);
                if (stopped) return;
                Paint(data);
                failures = 0;
                timer.Interval = 60000;
                status.Text = data.FiveHours.HasValue || data.Week.HasValue ? "Actualizado · cada 60 s" : Provider.Service + " no comunica estos límites para esta cuenta.";
            }
            catch (Exception ex)
            {
                if (stopped) return;
                Paint(new Snapshot()); // Never present stale values as live usage.
                status.Text = ex is InvalidOperationException ? ex.Message : "Sin conexión o respuesta inválida; reintentando.";
                failures = Math.Min(failures + 1, 4);
                timer.Interval = Math.Min(600000, 60000 * (1 << failures));
            }
            finally { busy = false; }
        }

        private void Paint(Snapshot data)
        {
            using (var bitmap = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                DrawSmallBar(g, data.FiveHours, 5);
                DrawSmallBar(g, data.Week, 19);
                IntPtr handle = bitmap.GetHicon();
                Icon next;
                try { using (var borrowed = Icon.FromHandle(handle)) next = (Icon)borrowed.Clone(); }
                finally { DestroyIcon(handle); }
                tray.Icon = next;
                if (currentIcon != null) currentIcon.Dispose();
                currentIcon = next;
            }
            tray.Text = "5 h: " + Usage.Percent(data.FiveHours) + "\nSemana: " + Usage.Percent(data.Week);
            popup.Data = data;
            popup.Invalidate();
        }

        private static void DrawSmallBar(Graphics g, double? value, int y)
        {
            using (var edge = new Pen(Color.FromArgb(190, 195, 205)))
            using (var track = new SolidBrush(Color.FromArgb(52, 57, 66)))
            using (var fill = new SolidBrush(Usage.BarColor(value)))
            {
                g.FillRectangle(track, 1, y, 30, 9);
                if (value.HasValue) g.FillRectangle(fill, 2, y + 1, (float)(28 * value.Value / 100), 7);
                else g.FillRectangle(fill, 11, y + 4, 10, 1);
                g.DrawRectangle(edge, 1, y, 29, 8);
            }
        }

        protected override void ExitThreadCore()
        {
            stopped = true;
            timer.Stop();
            timer.Dispose();
            tray.Visible = false;
            tray.Dispose();
            if (currentIcon != null) currentIcon.Dispose();
            popup.Dispose();
            client.Dispose();
            base.ExitThreadCore();
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool created;
            using (var mutex = new Mutex(true, "Local\\" + Provider.InstanceName, out created))
            {
                if (!created) return;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApp());
                GC.KeepAlive(mutex);
            }
        }
    }
}
