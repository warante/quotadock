using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UsageTray
{
    public sealed class BarsPopup : Form
    {
        public Meter[] Data = new Snapshot().Visible(false);
        public Color Accent = Color.WhiteSmoke;
        public string ProviderId;
        public BarsPopup()
        {
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
            StartPosition = FormStartPosition.Manual; ClientSize = new Size(340, 76);
            BackColor = Color.FromArgb(28, 30, 35); DoubleBuffered = true;
            Deactivate += delegate { Hide(); };
            KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Hide(); };
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var font = new Font("Segoe UI", 9))
            using (var text = new SolidBrush(Color.WhiteSmoke))
            using (var track = new SolidBrush(Color.FromArgb(62, 66, 76)))
            using (var accent = new SolidBrush(Accent))
            {
                e.Graphics.FillRectangle(accent, 0, 0, 3, Height);
                for (int i = 0; i < 2; i++)
                {
                    int y = 13 + 32 * i;
                    e.Graphics.DrawString(Data[i].Label, font, text, 12, y - 2);
                    e.Graphics.FillRectangle(track, 108, y + 2, 155, 12);
                    using (var fill = new SolidBrush(Visuals.BarColor(Data[i].Remaining)))
                        if (Data[i].Remaining.HasValue) e.Graphics.FillRectangle(fill, 108, y + 2, (float)(155 * Data[i].Remaining.Value / 100), 12);
                    e.Graphics.DrawString(Visuals.Percent(Data[i].Remaining), font, text, 278, y - 2);
                }
            }
        }
        public void Toggle(Meter[] data, Color accent, string id)
        {
            if (Visible && ProviderId == id) { Hide(); return; }
            Data = data; Accent = accent; ProviderId = id;
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(Math.Max(area.Left, Math.Min(Cursor.Position.X - Width / 2, area.Right - Width)), Math.Max(area.Top, Math.Min(Cursor.Position.Y - Height - 12, area.Bottom - Height - 8)));
            Show(); Activate(); Invalidate();
        }
    }

    public static class Visuals
    {
        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);
        public static string Percent(double? value) { return value.HasValue ? Math.Round(value.Value).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%" : "—%"; }
        public static Color BarColor(double? value) { return !value.HasValue ? Color.Gray : value <= 10 ? Color.FromArgb(246, 94, 94) : value <= 25 ? Color.FromArgb(245, 185, 66) : Color.FromArgb(61, 211, 158); }
        public static Icon Icon(ProviderDefinition p, Meter[] data)
        {
            using (var bitmap = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                using (var accent = new SolidBrush(p == null ? Color.Silver : p.Accent)) g.FillRectangle(accent, 0, 1, 3, 30);
                for (int i = 0; i < 2; i++)
                {
                    int y = 5 + i * 14;
                    using (var track = new SolidBrush(Color.FromArgb(52, 57, 66))) g.FillRectangle(track, 5, y, 26, 9);
                    using (var fill = new SolidBrush(BarColor(data[i].Remaining)))
                        if (data[i].Remaining.HasValue) g.FillRectangle(fill, 6, y + 1, (float)(24 * data[i].Remaining.Value / 100), 7);
                    using (var border = new Pen(Color.LightGray)) g.DrawRectangle(border, 5, y, 25, 8);
                }
                IntPtr handle = bitmap.GetHicon();
                try { using (var borrowed = System.Drawing.Icon.FromHandle(handle)) return (Icon)borrowed.Clone(); }
                finally { DestroyIcon(handle); }
            }
        }
    }

    internal sealed class ProviderState : IDisposable
    {
        public readonly ProviderDefinition Provider;
        public readonly NotifyIcon Tray = new NotifyIcon();
        public readonly RetryPolicy Policy;
        public readonly AlertGate Gate = new AlertGate();
        public readonly CancellationTokenSource Cancel = new CancellationTokenSource();
        public Snapshot Data = new Snapshot();
        public string Status = "Esperando primera consulta";
        public string LastErrorKind;
        public int CredentialGeneration;
        public bool Busy, Disposed;
        private Icon icon;
        public ProviderState(ProviderDefinition p, RetryPolicy policy) { Provider = p; Policy = policy; Data = Snapshot.Empty(p.Kind); }
        public void Paint(bool monthly)
        {
            var data = Data.Visible(monthly);
            var next = Visuals.Icon(Provider, data); Tray.Icon = next;
            if (icon != null) icon.Dispose(); icon = next;
            // NotifyIcon.Text is limited to 63 characters on .NET Framework.
            string tip = Provider.Name + "\n" + data[0].Label + ": " + Visuals.Percent(data[0].Remaining) + " · " + data[1].Label + ": " + Visuals.Percent(data[1].Remaining);
            Tray.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip;
        }
        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true; Cancel.Cancel(); Tray.Visible = false;
            if (Tray.ContextMenuStrip != null) Tray.ContextMenuStrip.Dispose();
            Tray.Dispose(); if (icon != null) icon.Dispose();
            // Token source is disposed after any pending request in Refresh's finally block.
            if (!Busy) Cancel.Dispose();
        }
    }

    public sealed class TrayApp : ApplicationContext
    {
        private readonly SettingsStore store = new SettingsStore();
        private readonly CredentialStore credentials;
        private Preferences prefs;
        private readonly Dictionary<string, ProviderState> states = new Dictionary<string, ProviderState>();
        private readonly Dictionary<string, RetryPolicy> policies = new Dictionary<string, RetryPolicy>();
        private readonly BarsPopup popup = new BarsPopup();
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private readonly HttpClient client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        private readonly NotifyIcon empty = new NotifyIcon();
        private Icon emptyIcon;
        private bool stopped;

        public TrayApp()
        {
            credentials = new CredentialStore(store.DirectoryPath);
            try { prefs = store.Load(); }
            catch { prefs = new Preferences(); MessageBox.Show("No se pudo leer la configuración. Se usarán los valores predeterminados; el archivo no se modifica hasta que cambies una opción.", "QuotaDock"); }
            try { foreach (var pair in store.LoadSchedules()) policies[pair.Key] = pair.Value; }
            catch { MessageBox.Show("No se pudo recuperar la espera de las consultas anteriores. Evita reiniciar repetidamente la aplicación si el proveedor limita las consultas.", "QuotaDock"); }
            client.Timeout = TimeSpan.FromSeconds(20);
            emptyIcon = Visuals.Icon(null, new Snapshot().Visible(false)); empty.Icon = emptyIcon; empty.Text = "QuotaDock · selecciona proveedores";
            empty.ContextMenuStrip = Menu(null); empty.MouseClick += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) empty.ContextMenuStrip.Show(Cursor.Position); };
            Apply();
            timer.Interval = 5000; timer.Tick += async delegate { await Tick(); }; timer.Start();
            EventHandler initial = null;
            initial = async delegate { Application.Idle -= initial; await Tick(); };
            Application.Idle += initial;
        }

        private bool Monthly(ProviderDefinition p) { return prefs.Monthly.Contains(p.Id); }
        private ContextMenuStrip Menu(ProviderState state)
        {
            var menu = new ContextMenuStrip();
            if (state != null)
            {
                menu.Items.Add(new ToolStripMenuItem(state.Provider.Name) { Enabled = false });
                menu.Items.Add(new ToolStripMenuItem(state.Status) { Name = "status", Enabled = false });
                menu.Items.Add("Actualizar (respeta la espera)", null, async delegate {
                    if (!state.Policy.CanFetch(DateTimeOffset.UtcNow)) { MessageBox.Show("La siguiente consulta estará disponible en " + Math.Max(1, Math.Ceiling((state.Policy.Next - DateTimeOffset.UtcNow).TotalSeconds)) + " s.", "QuotaDock"); return; }
                    await Refresh(state);
                });
                var monthly = new ToolStripMenuItem("Priorizar mes en la segunda barra") { Checked = Monthly(state.Provider) };
                monthly.Click += delegate { Change(delegate { prefs.Monthly = Monthly(state.Provider) ? prefs.Monthly.Where(id => id != state.Provider.Id).ToArray() : prefs.Monthly.Concat(new[] { state.Provider.Id }).ToArray(); }); };
                menu.Items.Add(monthly);
                if (state.Provider.AcceptsKey) menu.Items.Add("Configurar credencial…", null, delegate { ConfigureKey(state); });
                menu.Items.Add("Cómo conectar", null, delegate { MessageBox.Show(state.Provider.Help + "\n\nLa sesión se lee de nuevo en cada consulta. Nunca se modifica la del cliente oficial.", state.Provider.Name); });
                menu.Items.Add(new ToolStripSeparator());
            }
            var providers = new ToolStripMenuItem("Proveedores");
            foreach (var p in Providers.All)
            {
                var item = new ToolStripMenuItem(p.Name) { Checked = prefs != null && prefs.Enabled.Contains(p.Id) };
                item.Click += delegate { Change(delegate { prefs.Enabled = prefs.Enabled.Contains(p.Id) ? prefs.Enabled.Where(id => id != p.Id).ToArray() : prefs.Enabled.Concat(new[] { p.Id }).ToArray(); }); };
                providers.DropDownItems.Add(item);
            }
            menu.Items.Add(providers);
            var auto = new ToolStripMenuItem("Iniciar con Windows") { Checked = AutoStart.Enabled };
            auto.Click += delegate {
                try { AutoStart.Set(!AutoStart.Enabled, Application.ExecutablePath); RebuildMenus(); }
                catch { MessageBox.Show("No se pudo cambiar el inicio automático para tu usuario.", "QuotaDock"); }
            };
            menu.Items.Add(auto);
            var alerts = new ToolStripMenuItem("Avisar cuando quede poca cuota") { Checked = prefs != null && prefs.Alerts };
            alerts.Click += delegate { Change(delegate { prefs.Alerts = !prefs.Alerts; foreach (var s in states.Values) s.Gate.Reset(); }); };
            menu.Items.Add(alerts);
            var thresholds = new ToolStripMenuItem("Umbral de aviso");
            foreach (var number in new[] { 5, 10, 20, 25 })
            {
                var item = new ToolStripMenuItem(number + "% restante") { Checked = prefs != null && prefs.Threshold == number };
                item.Click += delegate { Change(delegate { prefs.Threshold = number; foreach (var s in states.Values) s.Gate.Reset(); }); };
                thresholds.DropDownItems.Add(item);
            }
            menu.Items.Add(thresholds);
            menu.Items.Add("Acerca de", null, delegate { MessageBox.Show("QuotaDock 1.1.0\nDos barras por proveedor. Sin telemetría.\nCredenciales manuales cifradas para tu usuario de Windows.\n\nLas cuotas proceden del proveedor, no de estimaciones. Los endpoints internos pueden cambiar.", "QuotaDock"); });
            menu.Items.Add("Salir", null, delegate { ExitThread(); });
            menu.Opening += delegate {
                if (state != null) menu.Items["status"].Text = state.Status;
            };
            return menu;
        }

        private void Change(Action action)
        {
            Preferences previous = new Preferences { Enabled = prefs.Enabled, Monthly = prefs.Monthly, Alerts = prefs.Alerts, Threshold = prefs.Threshold };
            try { action(); store.Save(prefs); Apply(); }
            catch { prefs = previous; MessageBox.Show("No se pudo guardar la configuración. Comprueba los permisos del perfil de Windows.", "QuotaDock"); }
        }
        private void RebuildMenus()
        {
            // Defer disposal of open menus until the click event has completed.
            foreach (var s in states.Values)
            {
                var old = s.Tray.ContextMenuStrip; s.Tray.ContextMenuStrip = Menu(s);
                if (old != null) timerDisposals.Add(old);
            }
            var previous = empty.ContextMenuStrip; empty.ContextMenuStrip = Menu(null);
            if (previous != null) timerDisposals.Add(previous);
        }
        private readonly List<IDisposable> timerDisposals = new List<IDisposable>();
        private void Apply()
        {
            popup.Hide();
            foreach (var id in states.Keys.Where(id => !prefs.Enabled.Contains(id)).ToArray()) { states[id].Dispose(); states.Remove(id); }
            foreach (var p in Providers.All.Where(p => prefs.Enabled.Contains(p.Id)))
            {
                if (!states.ContainsKey(p.Id))
                {
                    if (!policies.ContainsKey(p.Id)) policies[p.Id] = new RetryPolicy();
                    var state = new ProviderState(p, policies[p.Id]); states.Add(p.Id, state);
                    state.Tray.MouseClick += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) popup.Toggle(state.Data.Visible(Monthly(p)), p.Accent, p.Id); };
                    state.Tray.Visible = false;
                }
                states[p.Id].Paint(Monthly(p)); states[p.Id].Tray.Visible = true;
            }
            empty.Visible = states.Count == 0; RebuildMenus();
        }
        private void ConfigureKey(ProviderState state)
        {
            using (var dialog = new Form { Text = state.Provider.Name, ClientSize = new Size(430, 220), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, StartPosition = FormStartPosition.CenterScreen })
            {
                var info = new Label { Text = "La clave se cifrará con Windows para tu usuario.\nNunca se incluye en el proyecto ni se muestra después.\nUna variable de entorno tiene prioridad sobre la clave guardada.", Left = 15, Top = 15, Width = 400, Height = 65 };
                var box = new TextBox { Left = 15, Top = 85, Width = 400, UseSystemPasswordChar = true, MaxLength = 16384 };
                var save = new Button { Text = "Guardar", Left = 185, Top = 155, Width = 110 };
                var delete = new Button { Text = "Olvidar clave", Left = 305, Top = 155, Width = 110 };
                save.Click += delegate {
                    if (string.IsNullOrWhiteSpace(box.Text)) return;
                    try { credentials.Save(state.Provider, box.Text); box.Clear(); state.CredentialGeneration++; state.Data = Snapshot.Empty(state.Provider.Kind); state.Gate.Reset(); if (state.LastErrorKind == "auth" || state.LastErrorKind == "setup") state.Policy.Next = DateTimeOffset.MinValue; state.Paint(Monthly(state.Provider)); UpdatePopup(state); dialog.DialogResult = DialogResult.OK; }
                    catch { MessageBox.Show("No se pudo guardar la credencial de forma segura.", "QuotaDock"); }
                };
                delete.Click += delegate {
                    try { credentials.Save(state.Provider, ""); box.Clear(); state.CredentialGeneration++; state.Data = Snapshot.Empty(state.Provider.Kind); state.Gate.Reset(); state.Paint(Monthly(state.Provider)); UpdatePopup(state); dialog.DialogResult = DialogResult.OK; }
                    catch { MessageBox.Show("No se pudo eliminar la credencial.", "QuotaDock"); }
                };
                dialog.Controls.AddRange(new Control[] { info, box, save, delete }); dialog.AcceptButton = save; dialog.ShowDialog(); box.Clear();
                // Do not reset a server cooldown just because credentials changed.
            }
        }
        private async Task Tick()
        {
            foreach (var disposable in timerDisposals.ToArray()) disposable.Dispose(); timerDisposals.Clear();
            await Task.WhenAll(states.Values.Where(s => !s.Busy && s.Policy.CanFetch(DateTimeOffset.UtcNow)).Select(Refresh).ToArray());
        }
        private async Task Refresh(ProviderState state)
        {
            if (stopped || state.Disposed || state.Busy || !state.Policy.CanFetch(DateTimeOffset.UtcNow)) return;
            state.Busy = true;
            int generation = state.CredentialGeneration;
            // Reserve the next slot before starting I/O, so toggling the provider or restarting
            // during a pending request cannot issue another query immediately.
            state.Policy.Next = DateTimeOffset.UtcNow.AddSeconds(state.Provider.PollSeconds);
            PersistPolicies(state);
            try
            {
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(state.Cancel.Token))
                {
                    timeout.CancelAfter(TimeSpan.FromSeconds(25));
                    var data = await Usage.Fetch(state.Provider, client, credentials, timeout.Token);
                    if (stopped || state.Disposed) return;
                    if (generation != state.CredentialGeneration)
                    {
                        state.Policy.Success(DateTimeOffset.UtcNow, state.Provider.PollSeconds);
                        state.Status = "Credencial cambiada; esperando la próxima consulta.";
                        PersistPolicies(state); return; // Never paint a pending response from the previous login.
                    }
                    state.Data = data; state.LastErrorKind = null; state.Status = "Actualizado · intervalo " + state.Provider.PollSeconds + " s";
                    state.Policy.Success(DateTimeOffset.UtcNow, state.Provider.PollSeconds);
                    PersistPolicies(state);
                    state.Paint(Monthly(state.Provider));
                    UpdatePopup(state);
                    var crossed = data.Meters.Where(m => state.Gate.Observe(m, prefs.Threshold)).ToArray();
                    if (prefs.Alerts && crossed.Any()) state.Tray.ShowBalloonTip(7000, state.Provider.Name,
                        string.Join(" · ", crossed.Select(m => m.Label + ": " + Visuals.Percent(m.Remaining))), ToolTipIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                if (stopped || state.Disposed) return;
                var quota = ex as QuotaException;
                state.LastErrorKind = quota == null ? "network" : quota.Kind;
                state.Data = Snapshot.Empty(state.Provider.Kind);
                state.Status = quota == null ? (ex is OperationCanceledException ? "Tiempo de espera agotado; reintentando." : ex is HttpRequestException ? "Sin conexión; reintentando." : "No se pudo leer la sesión o la respuesta; comprueba la configuración.") : quota.Message;
                state.Policy.Fail(DateTimeOffset.UtcNow, state.Provider.PollSeconds, quota); PersistPolicies(state); state.Paint(Monthly(state.Provider)); UpdatePopup(state);
            }
            finally { state.Busy = false; if (state.Disposed) state.Cancel.Dispose(); }
        }
        private void UpdatePopup(ProviderState state)
        {
            if (popup.Visible && popup.ProviderId == state.Provider.Id) { popup.Data = state.Data.Visible(Monthly(state.Provider)); popup.Invalidate(); }
        }
        private void PersistPolicies(ProviderState state)
        {
            try { store.SaveSchedules(policies); }
            catch { state.Status += " · no se pudo guardar la espera para el próximo inicio"; }
        }
        protected override void ExitThreadCore()
        {
            stopped = true; timer.Stop(); timer.Dispose();
            foreach (var state in states.Values) state.Dispose();
            foreach (var menu in timerDisposals) menu.Dispose();
            empty.Visible = false; empty.ContextMenuStrip.Dispose(); empty.Dispose(); emptyIcon.Dispose();
            popup.Dispose(); client.Dispose(); base.ExitThreadCore();
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool created;
            using (var mutex = new Mutex(true, "Local\\QuotaDock.Unified", out created))
            {
                if (!created) return;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApp()); GC.KeepAlive(mutex);
            }
        }
    }
}
