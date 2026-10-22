using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using UsageTray;

internal static class VisualTests
{
    [DllImport("user32.dll")] private static extern int GetGuiResources(IntPtr process, int flag);
    [STAThread]
    public static int Main()
    {
        try
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            using (var popup = new BarsPopup())
            {
                popup.Data = new[] { new Meter("session", "5 h", 81), new Meter("weekly", "Semana", 9) };
                popup.Accent = Color.MediumSeaGreen;
                using (var bitmap = new Bitmap(340, 76))
                {
                    popup.DrawToBitmap(bitmap, new Rectangle(0, 0, 340, 76));
                    if (bitmap.GetPixel(110, 17).G <= bitmap.GetPixel(110, 17).R) throw new Exception("First bar not green");
                    if (bitmap.GetPixel(110, 49).R <= bitmap.GetPixel(110, 49).G) throw new Exception("Second bar not red");
                    bitmap.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preview.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                for (int i = 0; i < 5; i++) using (var icon = Visuals.Icon(Providers.All[0], popup.Data)) { }
                int before = GetGuiResources(Process.GetCurrentProcess().Handle, 0);
                for (int i = 0; i < 200; i++) using (var icon = Visuals.Icon(Providers.All[i % Providers.All.Length], popup.Data)) { }
                GC.Collect(); GC.WaitForPendingFinalizers();
                int after = GetGuiResources(Process.GetCurrentProcess().Handle, 0);
                if (after > before + 10) throw new Exception("GDI resource leak");
            }
            Console.WriteLine("OK: two-line popup rendering, quota colors and 200 icon updates without GDI leak."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
    }
}
