using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using UsageTray;

internal sealed class Installer : Form
{
    private readonly CheckBox startup = new CheckBox { Text = "Abrir QuotaDock al iniciar Windows", Left = 24, Top = 95, Width = 330 };
    private readonly CheckBox launch = new CheckBox { Text = "Abrir QuotaDock al finalizar", Checked = true, Left = 24, Top = 125, Width = 330 };
    private readonly Button install = new Button { Text = "Instalar", Left = 245, Top = 175, Width = 110, Height = 32 };
    private Installer()
    {
        Text = "QuotaDock 1.1.0"; ClientSize = new Size(390, 230);
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 9);
        Controls.Add(new Label { Text = "Todos tus proveedores en una sola aplicación.\nInstalación para tu usuario, sin administrador.\nSelecciona proveedores y alertas desde el icono.", Left = 24, Top = 16, Width = 350, Height = 65 });
        Controls.AddRange(new Control[] { startup, launch, install });
        install.Click += delegate {
            install.Enabled = false;
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuotaDock");
            try
            {
                string programs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "QuotaDock");
                string powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
                new InstallEngine(new WindowsInstallPlatform()).Install(root, programs, startup.Checked, powershell, Resource);
            }
            catch { MessageBox.Show("La instalación no se ha completado. Se ha intentado restaurar el estado anterior. Cierra QuotaDock antes de actualizar y comprueba los permisos de tu perfil.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error); install.Enabled = true; return; }

            // Installation has already succeeded: a blocked launch must never roll it back
            // or be reported as a failure to install.
            if (launch.Checked)
            {
                var outcome = InstalledAppLauncher.TryStart(Path.Combine(root, "QuotaDock.exe"), path => {
                    using (var process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })) { }
                });
                if (outcome != LaunchOutcome.Started)
                {
                    MessageBox.Show(outcome == LaunchOutcome.BlockedByPolicy ?
                        "QuotaDock está instalado, pero Windows ha bloqueado su ejecución por una política de control de aplicaciones o de firma.\n\nNo es un error de copia de archivos. Los ejecutables actuales no están firmados. Solicita aprobación al administrador o utiliza una distribución firmada admitida por tu equipo. No desactives las protecciones.\n\nEl acceso de QuotaDock está disponible en el menú Inicio." :
                        "QuotaDock está instalado, pero no se pudo abrir automáticamente. Prueba el acceso QuotaDock del menú Inicio para ver si Windows muestra un bloqueo o algún otro error.",
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close(); return;
                }
            }
            MessageBox.Show(launch.Checked ?
                "QuotaDock instalado y abierto. Busca sus dos barras junto al reloj o en la flecha ^ de iconos ocultos.\n\nSi usabas la versión 1.0, sal de sus iconos y retira sus antiguos accesos de inicio. No se han modificado tus sesiones ni esas copias portables." :
                "QuotaDock instalado. Ábrelo desde el menú Inicio cuando quieras.\n\nSi usabas la versión 1.0, sal de sus iconos y retira sus antiguos accesos de inicio.", Text);
            Close();
        };
    }
    private static byte[] Resource(string name)
    {
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        {
            if (stream == null) throw new InvalidOperationException("Recurso ausente.");
            using (var memory = new MemoryStream()) { stream.CopyTo(memory); return memory.ToArray(); }
        }
    }
    [STAThread]
    private static void Main()
    {
        bool created;
        using (var mutex = new System.Threading.Mutex(true, "Local\\QuotaDock.Installer", out created))
        {
            if (!created) return;
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Installer()); GC.KeepAlive(mutex);
        }
    }
}
