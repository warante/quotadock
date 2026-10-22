using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

internal sealed class Installer : Form
{
    private readonly CheckBox openai = new CheckBox { Text = "OpenAI / Codex", Checked = true, Left = 24, Top = 65, Width = 250 };
    private readonly CheckBox claude = new CheckBox { Text = "Claude / Claude Code", Checked = true, Left = 24, Top = 95, Width = 250 };
    private readonly CheckBox startup = new CheckBox { Text = "Abrir los seleccionados al iniciar Windows", Left = 24, Top = 135, Width = 330 };
    private readonly Button install = new Button { Text = "Instalar", Left = 245, Top = 185, Width = 110, Height = 32 };

    private Installer()
    {
        Text = "QuotaDock 1.0.0";
        ClientSize = new Size(380, 240);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9);
        Controls.Add(new Label { Text = "Tus límites de IA, de un vistazo.\nInstalación solo para tu usuario, sin administrador.", Left = 24, Top = 16, Width = 340, Height = 42 });
        Controls.AddRange(new Control[] { openai, claude, startup, install });
        install.Click += delegate { Install(); };
    }

    private static void Extract(string resource, string target)
    {
        using (var source = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
        {
            if (source == null) throw new InvalidOperationException("Recurso no disponible: " + resource);
            using (var output = new FileStream(target, FileMode.Create, FileAccess.Write)) source.CopyTo(output);
        }
    }

    private static void Shortcut(string path, string target, string arguments)
    {
        dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
        dynamic shortcut = shell.CreateShortcut(path);
        shortcut.TargetPath = target;
        shortcut.Arguments = arguments;
        shortcut.WorkingDirectory = Path.GetDirectoryName(target);
        shortcut.Description = "QuotaDock — límites de suscripción";
        shortcut.Save();
    }

    private void Install()
    {
        if (!openai.Checked && !claude.Checked) { MessageBox.Show("Selecciona al menos un proveedor."); return; }
        install.Enabled = false;
        try
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuotaDock");
            string programs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "QuotaDock");
            string start = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            Directory.CreateDirectory(directory);
            Directory.CreateDirectory(programs);
            string[] names = { "OpenAI", "Claude" };
            bool[] selected = { openai.Checked, claude.Checked };
            for (int i = 0; i < names.Length; i++)
            {
                if (!selected[i]) continue;
                string name = "QuotaDock." + names[i];
                string target = Path.Combine(directory, name + ".exe");
                Extract(name + ".exe", target);
                Shortcut(Path.Combine(programs, "QuotaDock - " + names[i] + ".lnk"), target, "");
                string startupLink = Path.Combine(start, name + ".lnk");
                if (startup.Checked) Shortcut(startupLink, target, "");
                else if (File.Exists(startupLink)) File.Delete(startupLink);
            }
            Extract("Uninstall.ps1", Path.Combine(directory, "Uninstall.ps1"));
            string powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
            string arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + Path.Combine(directory, "Uninstall.ps1") + "\"";
            Shortcut(Path.Combine(programs, "Desinstalar QuotaDock.lnk"), powershell, arguments);
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\QuotaDock"))
            {
                key.SetValue("DisplayName", "QuotaDock");
                key.SetValue("DisplayVersion", "1.0.0");
                key.SetValue("Publisher", "QuotaDock");
                key.SetValue("InstallLocation", directory);
                key.SetValue("UninstallString", "\"" + powershell + "\" " + arguments);
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
            MessageBox.Show("QuotaDock instalado. Ábrelo desde el menú Inicio.\n\nNecesitas una sesión previa de Codex o Claude Code.\nLas credenciales y las versiones portables anteriores no se modifican.", Text);
            Close();
        }
        catch (Exception)
        {
            MessageBox.Show("No se pudo completar la instalación. Cierra QuotaDock si está abierto y comprueba los permisos de tu carpeta de usuario. Puede haber archivos de una instalación parcial; vuelve a ejecutar el instalador.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            install.Enabled = true;
        }
    }

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Installer());
    }
}
