using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace UsageTray
{
    // Native GDI+ drawing only: no web runtime, animation loop or external assets.
    internal static class Ui
    {
        public static Color Panel { get { return SystemInformation.HighContrast ? SystemColors.Window : Color.FromArgb(27, 31, 39); } }
        public static Color Text { get { return SystemInformation.HighContrast ? SystemColors.WindowText : Color.FromArgb(236, 240, 247); } }
        public static Color Muted { get { return SystemInformation.HighContrast ? SystemColors.WindowText : Color.FromArgb(161, 172, 191); } }
        public static Color Border { get { return SystemInformation.HighContrast ? SystemColors.WindowText : Color.FromArgb(57, 65, 80); } }
        public static Color Track { get { return SystemInformation.HighContrast ? SystemColors.ControlDark : Color.FromArgb(53, 61, 75); } }
        public static Color Blend(Color a, Color b, float amount)
        {
            return Color.FromArgb((int)(a.R + (b.R - a.R) * amount), (int)(a.G + (b.G - a.G) * amount), (int)(a.B + (b.B - a.B) * amount));
        }
        public static GraphicsPath Rounded(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            float size = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            if (size <= 0) { path.AddRectangle(bounds); return path; }
            path.AddArc(bounds.Left, bounds.Top, size, size, 180, 90);
            path.AddArc(bounds.Right - size, bounds.Top, size, size, 270, 90);
            path.AddArc(bounds.Right - size, bounds.Bottom - size, size, size, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - size, size, size, 90, 90);
            path.CloseFigure(); return path;
        }
        public static void Fill(Graphics g, Color color, RectangleF bounds, float radius)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            using (var path = Rounded(bounds, radius))
            using (var brush = new SolidBrush(color)) g.FillPath(brush, path);
        }
        public static void Outline(Graphics g, Color color, RectangleF bounds, float radius)
        {
            using (var path = Rounded(bounds, radius))
            using (var pen = new Pen(color)) g.DrawPath(pen, path);
        }
        public static void StyleMenu(ContextMenuStrip menu)
        {
            if (SystemInformation.HighContrast) return;
            menu.BackColor = Panel; menu.ForeColor = Text;
            menu.Renderer = new DarkMenuRenderer();
        }

        private sealed class DarkColors : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground { get { return Panel; } }
            public override Color ImageMarginGradientBegin { get { return Panel; } }
            public override Color ImageMarginGradientMiddle { get { return Panel; } }
            public override Color ImageMarginGradientEnd { get { return Panel; } }
            public override Color MenuBorder { get { return Border; } }
            public override Color MenuItemBorder { get { return Border; } }
            public override Color MenuItemSelected { get { return Track; } }
            public override Color MenuItemSelectedGradientBegin { get { return Track; } }
            public override Color MenuItemSelectedGradientEnd { get { return Track; } }
            public override Color MenuItemPressedGradientBegin { get { return Track; } }
            public override Color MenuItemPressedGradientMiddle { get { return Track; } }
            public override Color MenuItemPressedGradientEnd { get { return Track; } }
            public override Color CheckBackground { get { return Track; } }
            public override Color CheckSelectedBackground { get { return Track; } }
            public override Color CheckPressedBackground { get { return Track; } }
            public override Color SeparatorDark { get { return Border; } }
            public override Color SeparatorLight { get { return Panel; } }
        }
        private sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
        {
            public DarkMenuRenderer() : base(new DarkColors()) { RoundedEdges = true; }
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Enabled ? Text : Muted; base.OnRenderItemText(e);
            }
            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                e.ArrowColor = e.Item.Enabled ? Text : Muted; base.OnRenderArrow(e);
            }
            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                using (var pen = new Pen(Text, 1.7f))
                {
                    var r = e.ImageRectangle;
                    e.Graphics.DrawLines(pen, new[] { new PointF(r.Left + 3, r.Top + r.Height / 2f), new PointF(r.Left + 6, r.Bottom - 4), new PointF(r.Right - 2, r.Top + 3) });
                }
            }
        }
    }

    internal sealed class ProviderTab : Button
    {
        private bool hovered, pressed, selected;
        public Color Accent;
        public bool Selected { get { return selected; } set { if (selected != value) { selected = value; Invalidate(); } } }
        public ProviderTab()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat; UseVisualStyleBackColor = false; Cursor = Cursors.Hand;
            FlatAppearance.BorderSize = 0;
        }
        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.Clear(Ui.Panel); g.SmoothingMode = SmoothingMode.AntiAlias;
            bool contrast = SystemInformation.HighContrast;
            Color accent = contrast ? SystemColors.Highlight : Accent;
            Color fill = Selected ? Ui.Blend(Ui.Panel, accent, .17f) : Ui.Blend(Ui.Panel, Ui.Text, hovered ? .09f : .035f);
            if (pressed) fill = Ui.Blend(fill, accent, .1f);
            if (contrast) fill = Selected ? SystemColors.Highlight : SystemColors.Window;
            var bounds = new RectangleF(.5f, .5f, Width - 1, Height - 1);
            Ui.Fill(g, fill, bounds, 8);
            Ui.Outline(g, Selected || (Focused && ShowFocusCues) ? accent : Ui.Border, bounds, 8);
            Color text = contrast ? (Selected ? SystemColors.HighlightText : SystemColors.WindowText) : Selected ? Ui.Text : Ui.Muted;
            Ui.Fill(g, accent, new RectangleF(13, (Height - 5) / 2f, 5, 5), 2.5f);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(25, 0, Width - 31, Height), text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, new Rectangle(5, 5, Width - 10, Height - 10), text, fill);
        }
    }
}
