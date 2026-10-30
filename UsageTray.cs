using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        public event Action<string> ProviderSelected;
        private readonly FlowLayoutPanel tabs = new FlowLayoutPanel();
        private string[] providerIds = new string[0];
        private readonly Font bodyFont = new Font("Segoe UI", 9.5f);
        private readonly Font valueFont = new Font("Segoe UI Semibold", 10);
        private readonly ToolTip infoTip = new ToolTip { InitialDelay = 300, ReshowDelay = 100, AutoPopDelay = 15000, ShowAlways = true };
        private readonly Label[] infoIcons = new Label[2];
        internal const int RowHeight = 34;
        public int BarsTop { get; private set; }
        public BarsPopup()
        {
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
            StartPosition = FormStartPosition.Manual; ClientSize = new Size(410, 140);
            BackColor = Ui.Panel; DoubleBuffered = true; Font = bodyFont;
            KeyPreview = true;
            tabs.Location = new Point(14, 14); tabs.Width = 382;
            tabs.WrapContents = true; tabs.FlowDirection = FlowDirection.LeftToRight;
            Controls.Add(tabs); BarsTop = 64;
            for (int i = 0; i < infoIcons.Length; i++)
            {
                int index = i;
                var icon = new Label { Text = "ⓘ", ForeColor = Ui.Muted, BackColor = Ui.Panel,
                    TextAlign = ContentAlignment.MiddleCenter, Size = new Size(22, 24),
                    AccessibleName = "Información del reinicio", Cursor = Cursors.Help };
                infoIcons[i] = icon; Controls.Add(icon);
                icon.MouseEnter += delegate { infoTip.SetToolTip(icon, InfoTooltipText(index, DateTimeOffset.UtcNow)); };
            }
            PositionInfoIcons();
            Deactivate += delegate { Hide(); };
            KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Hide(); };
        }
        public void SetProviders(ProviderDefinition[] providers)
        {
            string[] ids = providers.Select(p => p.Id).ToArray();
            if (providerIds.SequenceEqual(ids)) return;
            providerIds = ids;
            tabs.SuspendLayout();
            foreach (Control control in tabs.Controls.Cast<Control>().ToArray()) { tabs.Controls.Remove(control); control.Dispose(); }
            foreach (var p in providers)
            {
                var button = new ProviderTab { Text = TabLabel(p), Tag = p.Id, Width = 121, Height = 34, Accent = p.Accent,
                    Margin = new Padding(3),
                    AccessibleName = "Mostrar " + p.Name };
                button.Click += delegate { var selected = ProviderSelected; if (selected != null) selected(p.Id); };
                tabs.Controls.Add(button);
            }
            int rows = Math.Max(1, (providers.Length + 2) / 3);
            tabs.Height = rows * 40; BarsTop = tabs.Bottom + 21;
            ClientSize = new Size(410, BarsTop + 76);
            PositionInfoIcons();
            tabs.ResumeLayout(); UpdateTabs(); Invalidate();
        }
        private static string TabLabel(ProviderDefinition p)
        {
            switch (p.Id)
            {
                case "openai": return "OpenAI";
                case "opencode-go": return "OpenCode Go";
                case "kimi": return "Kimi";
                case "kimi-cn": return "Kimi CN";
                case "minimax": return "MiniMax";
                case "minimax-cn": return "MiniMax CN";
                case "zai": return "Z.AI";
                case "zhipu": return "Zhipu CN";
                case "copilot": return "Copilot";
                case "gemini": return "Gemini CLI";
                default: return p.Name;
            }
        }
        private void UpdateTabs()
        {
            foreach (ProviderTab button in tabs.Controls)
            {
                bool selected = (string)button.Tag == ProviderId;
                button.Selected = selected;
                button.AccessibleDescription = selected ? "Proveedor seleccionado" : "Cambiar proveedor visible";
            }
        }
        public void UpdateData(Meter[] data, Color accent, string id)
        {
            foreach (var icon in infoIcons) { infoTip.Hide(icon); infoTip.SetToolTip(icon, null); }
            Data = data; Accent = accent; ProviderId = id; UpdateTabs(); Invalidate();
        }
        private void PositionInfoIcons()
        {
            for (int i = 0; i < infoIcons.Length; i++) infoIcons[i].Location = new Point(379, BarsTop - 3 + RowHeight * i);
        }
        internal string InfoTooltipText(int index, DateTimeOffset now)
        {
            var meter = Data[index];
            return meter.Label + " · " + ResetInfo.Describe(meter.Remaining.HasValue ? meter.Reset : null, now);
        }
        internal string ValueLabel(int index, DateTimeOffset now)
        {
            var meter = Data[index];
            return Visuals.Percent(meter.Remaining) + " | " + ResetInfo.Compact(meter.Remaining.HasValue ? meter.Reset : null, meter.Id == "session", now);
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData == Keys.Left || keyData == Keys.Right) && providerIds.Length > 1)
            {
                int index = Array.IndexOf(providerIds, ProviderId);
                int next = (Math.Max(0, index) + (keyData == Keys.Right ? 1 : providerIds.Length - 1)) % providerIds.Length;
                var selected = ProviderSelected; if (selected != null) selected(providerIds[next]);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var divider = new Pen(Ui.Border)) g.DrawLine(divider, 20, BarsTop - 12, Width - 20, BarsTop - 12);
            Ui.Outline(g, Ui.Border, new RectangleF(.5f, .5f, Width - 1, Height - 1), 13);
            var now = DateTimeOffset.UtcNow;
            for (int i = 0; i < 2; i++)
            {
                int y = BarsTop + RowHeight * i;
                TextRenderer.DrawText(g, Data[i].Label, bodyFont, new Rectangle(21, y - 2, 94, 23), Ui.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                var track = new RectangleF(120, y + 5, 120, 8);
                Ui.Fill(g, Ui.Track, track, 4);
                if (Data[i].Remaining.HasValue && Data[i].Remaining.Value > 0)
                {
                    // Clip the full rounded fill instead of giving tiny quotas an artificial minimum width.
                    var saved = g.Save();
                    g.SetClip(new RectangleF(track.X, track.Y, (float)(track.Width * Data[i].Remaining.Value / 100), track.Height), CombineMode.Intersect);
                    Ui.Fill(g, SystemInformation.HighContrast ? SystemColors.Highlight : Visuals.BarColor(Data[i].Remaining), track, 4);
                    g.Restore(saved);
                }
                Color number = !SystemInformation.HighContrast && Data[i].Remaining <= 10 ? Visuals.BarColor(Data[i].Remaining) : Ui.Text;
                TextRenderer.DrawText(g, ValueLabel(i, now), valueFont, new Rectangle(250, y - 3, 124, 24), number, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }
        protected override CreateParams CreateParams
        {
            get { var value = base.CreateParams; value.ClassStyle |= 0x00020000; return value; } // Native window shadow; no extra renderer.
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Width < 1 || Height < 1) return;
            using (var shape = Ui.Rounded(new RectangleF(0, 0, Width, Height), 14))
            {
                var previous = Region; Region = new Region(shape); if (previous != null) previous.Dispose();
            }
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) { infoTip.Dispose(); bodyFont.Dispose(); valueFont.Dispose(); }
        }
        public void Toggle(Meter[] data, Color accent, string id)
        {
            if (Visible && ProviderId == id) { Hide(); return; }
            UpdateData(data, accent, id);
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
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Ui.Fill(g, p == null ? Color.Silver : p.Accent, new RectangleF(1, 3, 2, 26), 1);
                for (int i = 0; i < 2; i++)
                {
                    int y = 6 + i * 14;
                    var track = new RectangleF(6, y, 25, 7);
                    Ui.Fill(g, Color.FromArgb(63, 71, 85), track, 2);
                    if (data[i].Remaining.HasValue && data[i].Remaining.Value > 0)
                    {
                        var saved = g.Save();
                        g.SetClip(new RectangleF(6, y, (float)(25 * data[i].Remaining.Value / 100), 7), CombineMode.Intersect);
                        Ui.Fill(g, BarColor(data[i].Remaining), track, 2); g.Restore(saved);
                    }
                    Ui.Outline(g, Color.FromArgb(150, 163, 181), track, 2);
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
        public readonly RetryPolicy Policy;
        public readonly AlertGate Gate = new AlertGate();
        public readonly CancellationTokenSource Cancel = new CancellationTokenSource();
        public Snapshot Data = new Snapshot();
        public string Status = "Esperando primera consulta";
        public string LastErrorKind;
        public int CredentialGeneration;
        public bool Busy, Disposed;
        public ProviderState(ProviderDefinition p, RetryPolicy policy) { Provider = p; Policy = policy; Data = Snapshot.Empty(p.Kind); }
        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true; Cancel.Cancel();
            // Token source is disposed after any pending request in Refresh's finally block.
            if (!Busy) Cancel.Dispose();
        }
    }

    public sealed class TrayApp : ApplicationContext
    {
        private readonly SettingsStore store;
        private readonly Func<ProviderDefinition, CancellationToken, Task<Snapshot>> fetchOverride;
        private readonly CredentialStore credentials;
        private Preferences prefs;
        private readonly Dictionary<string, ProviderState> states = new Dictionary<string, ProviderState>();
        private readonly Dictionary<string, RetryPolicy> policies = new Dictionary<string, RetryPolicy>();
        private readonly BarsPopup popup = new BarsPopup();
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private readonly HttpClient client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        private readonly NotifyIcon tray = new NotifyIcon();
        private Icon currentIcon;
        private bool stopped;

        public TrayApp() : this(new SettingsStore(), null, true) { }
        internal TrayApp(SettingsStore settings, Func<ProviderDefinition, CancellationToken, Task<Snapshot>> fetch, bool startPolling)
        {
            store = settings; fetchOverride = fetch;
            credentials = new CredentialStore(store.DirectoryPath);
            try { prefs = store.Load(); }
            catch { prefs = new Preferences(); MessageBox.Show("No se pudo leer la configuración. Se usarán los valores predeterminados; el archivo no se modifica hasta que cambies una opción.", "QuotaDock"); }
            try { foreach (var pair in store.LoadSchedules()) policies[pair.Key] = pair.Value; }
            catch { MessageBox.Show("No se pudo recuperar la espera de las consultas anteriores. Evita reiniciar repetidamente la aplicación si el proveedor limita las consultas.", "QuotaDock"); }
            client.Timeout = TimeSpan.FromSeconds(20);
            popup.ProviderSelected += SelectProvider;
            tray.MouseClick += delegate(object sender, MouseEventArgs e) {
                if (e.Button != MouseButtons.Left) return;
                var state = SelectedState();
                if (state == null) { tray.ContextMenuStrip.Show(Cursor.Position); return; }
                popup.Toggle(state.Data.Visible(Monthly(state.Provider)), state.Provider.Accent, state.Provider.Id);
            };
            Apply();
            tray.Visible = true;
            timer.Interval = 5000; timer.Tick += async delegate { await Tick(); };
            if (!startPolling) return;
            timer.Start();
            EventHandler initial = null;
            initial = async delegate { Application.Idle -= initial; await Tick(); };
            Application.Idle += initial;
        }

        private bool Monthly(ProviderDefinition p) { return prefs.Monthly.Contains(p.Id); }
        private ProviderState SelectedState()
        {
            ProviderState state;
            string id = prefs.ResolveSelection();
            return id != null && states.TryGetValue(id, out state) ? state : null;
        }
        internal void SelectProvider(string id)
        {
            if (!states.ContainsKey(id) || prefs.SelectedProvider == id) return;
            string previous = prefs.SelectedProvider;
            try { prefs.SelectedProvider = id; store.Save(prefs); }
            catch { prefs.SelectedProvider = previous; MessageBox.Show("No se pudo guardar el proveedor seleccionado.", "QuotaDock"); return; }
            PaintSelected(); RebuildMenus();
            // Switching is purely visual: no extra requests and no reset of any cooldown.
        }
        internal string SelectedProviderId { get { return prefs.ResolveSelection(); } }
        internal string TrayText { get { return tray.Text; } }
        internal Task RefreshProvider(string id) { return Refresh(states[id]); }
        private void PaintSelected()
        {
            var state = SelectedState();
            var data = state == null ? new Snapshot().Visible(false) : state.Data.Visible(Monthly(state.Provider));
            var next = Visuals.Icon(state == null ? null : state.Provider, data);
            tray.Icon = next; if (currentIcon != null) currentIcon.Dispose(); currentIcon = next;
            string tip = state == null ? "QuotaDock · selecciona proveedores" : state.Provider.Name + "\n" + data[0].Label + ": " + Visuals.Percent(data[0].Remaining) + " · " + data[1].Label + ": " + Visuals.Percent(data[1].Remaining);
            tray.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip;
            if (state != null) popup.UpdateData(data, state.Provider.Accent, state.Provider.Id);
        }
        private ContextMenuStrip Menu(ProviderState state)
        {
            var menu = new ContextMenuStrip();
            Ui.StyleMenu(menu);
            if (state != null)
            {
                menu.Items.Add(new ToolStripMenuItem(state.Provider.Name) { Enabled = false });
                menu.Items.Add(new ToolStripMenuItem(state.Status) { Name = "status", Enabled = false });
                var resets = new ToolStripMenuItem("Restablecimientos · hora peninsular");
                for (int i = 0; i < 2; i++) resets.DropDownItems.Add(new ToolStripMenuItem { Enabled = false });
                resets.DropDownOpening += delegate {
                    var meters = state.Data.Visible(Monthly(state.Provider));
                    for (int i = 0; i < 2; i++) resets.DropDownItems[i].Text = meters[i].Label + " · " + ResetInfo.Describe(meters[i].Remaining.HasValue ? meters[i].Reset : null, DateTimeOffset.UtcNow);
                };
                menu.Items.Add(resets);
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
            var visible = new ToolStripMenuItem("Proveedor visible");
            foreach (var p in Providers.All.Where(p => prefs.Enabled.Contains(p.Id)))
            {
                var item = new ToolStripMenuItem(p.Name) { Checked = prefs.ResolveSelection() == p.Id };
                item.Click += delegate { SelectProvider(p.Id); };
                visible.DropDownItems.Add(item);
            }
            visible.Enabled = visible.DropDownItems.Count > 0;
            menu.Items.Add(visible);
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
            menu.Items.Add("Acerca de", null, delegate { MessageBox.Show("QuotaDock 1.3.0 · MIT\nUn icono, dos barras y selector de proveedor. Sin telemetría.\nPasa el ratón sobre una barra para consultar su reinicio.\n\nLas cuotas proceden del proveedor, no de estimaciones. Los endpoints internos pueden cambiar.", "QuotaDock"); });
            menu.Items.Add("Salir", null, delegate { ExitThread(); });
            menu.Opening += delegate {
                if (state != null) menu.Items["status"].Text = state.Status;
            };
            return menu;
        }

        private void Change(Action action)
        {
            Preferences previous = new Preferences { Enabled = prefs.Enabled, Monthly = prefs.Monthly, SelectedProvider = prefs.SelectedProvider, Alerts = prefs.Alerts, Threshold = prefs.Threshold };
            try { action(); prefs.SelectedProvider = prefs.ResolveSelection(); store.Save(prefs); Apply(); }
            catch { prefs = previous; MessageBox.Show("No se pudo guardar la configuración. Comprueba los permisos del perfil de Windows.", "QuotaDock"); }
        }
        private void RebuildMenus()
        {
            // Defer disposal of open menus until the click event has completed.
            var previous = tray.ContextMenuStrip; tray.ContextMenuStrip = Menu(SelectedState());
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
                }
            }
            prefs.SelectedProvider = prefs.ResolveSelection();
            popup.SetProviders(Providers.All.Where(p => states.ContainsKey(p.Id)).ToArray());
            PaintSelected(); RebuildMenus();
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
                    try { credentials.Save(state.Provider, box.Text); box.Clear(); state.CredentialGeneration++; state.Data = Snapshot.Empty(state.Provider.Kind); state.Gate.Reset(); if (state.LastErrorKind == "auth" || state.LastErrorKind == "setup") state.Policy.Next = DateTimeOffset.MinValue; UpdatePopup(state); dialog.DialogResult = DialogResult.OK; }
                    catch { MessageBox.Show("No se pudo guardar la credencial de forma segura.", "QuotaDock"); }
                };
                delete.Click += delegate {
                    try { credentials.Save(state.Provider, ""); box.Clear(); state.CredentialGeneration++; state.Data = Snapshot.Empty(state.Provider.Kind); state.Gate.Reset(); UpdatePopup(state); dialog.DialogResult = DialogResult.OK; }
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
                    var data = fetchOverride == null ? await Usage.Fetch(state.Provider, client, credentials, timeout.Token) : await fetchOverride(state.Provider, timeout.Token);
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
                    UpdatePopup(state);
                    var crossed = data.Meters.Where(m => state.Gate.Observe(m, prefs.Threshold)).ToArray();
                    if (prefs.Alerts && crossed.Any()) tray.ShowBalloonTip(7000, state.Provider.Name,
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
                state.Policy.Fail(DateTimeOffset.UtcNow, state.Provider.PollSeconds, quota); PersistPolicies(state); UpdatePopup(state);
            }
            finally { state.Busy = false; if (state.Disposed) state.Cancel.Dispose(); }
        }
        private void UpdatePopup(ProviderState state)
        {
            if (SelectedState() == state) PaintSelected();
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
            tray.Visible = false; tray.ContextMenuStrip.Dispose(); tray.Dispose(); if (currentIcon != null) currentIcon.Dispose();
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
