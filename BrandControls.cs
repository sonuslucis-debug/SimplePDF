using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace SimplePdfReader
{
    // Palette taken from the Sonus Lucis site stylesheet (style.css).
    internal static class Brand
    {
        public static readonly Color Navy = Color.FromArgb(14, 58, 91);
        public static readonly Color Accent = Color.FromArgb(46, 109, 164);
        public static readonly Color Ink = Color.FromArgb(43, 43, 43);
        public static readonly Color Paper = Color.White;

        public static readonly Color NavyHover = Color.FromArgb(20, 74, 114);
        public static readonly Color NavyPressed = Color.FromArgb(10, 44, 69);

        public static readonly Color PaleBlue = Color.FromArgb(233, 242, 249);
        public static readonly Color PaleBlueHover = Color.FromArgb(220, 234, 245);
        public static readonly Color PaleBorder = Color.FromArgb(198, 218, 233);
        public static readonly Color OnNavyMuted = Color.FromArgb(160, 198, 226);
        public static readonly Color DisabledText = Color.FromArgb(150, 168, 182);

        private const string PreferredFont = "Montserrat";
        private const string FallbackFont = "Segoe UI";

        // Montserrat is used by the site, but it may not be installed locally.
        public static Font Font(float size, FontStyle style)
        {
            if (IsFontInstalled(PreferredFont))
            {
                return new Font(PreferredFont, size, style);
            }
            return new Font(FallbackFont, size, style);
        }

        private static bool IsFontInstalled(string name)
        {
            try
            {
                using var installed = new InstalledFontCollection();
                foreach (FontFamily family in installed.Families)
                {
                    if (string.Equals(family.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Treat an enumeration failure as "not installed".
            }
            return false;
        }

        public static Image? LoadLogo()
        {
            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("SimplePdfReader.logo.png");
                if (stream == null)
                {
                    return null;
                }

                using var source = Image.FromStream(stream);
                return new Bitmap(source);
            }
            catch
            {
                return null;
            }
        }
    }


    internal enum BrandButtonStyle
    {
        Primary,
        Light
    }

    // Rounded flat button in the Sonus Lucis palette. It paints itself so the
    // corners match the site styling; text is drawn with GDI so it stays crisp.
    internal sealed class BrandButton : Button
    {
        private bool isChecked;
        private bool hovered;
        private bool pressed;
        private BrandButtonStyle style = BrandButtonStyle.Light;

        public BrandButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;
            Font = Brand.Font(9f, FontStyle.Bold);
            Height = 30;
            BackColor = Brand.Paper;
        }

        public BrandButtonStyle Style
        {
            get => style;
            set
            {
                style = value;
                Invalidate();
            }
        }

        public bool CheckOnClick { get; set; }

        public bool Checked
        {
            get => isChecked;
            set
            {
                if (isChecked == value)
                {
                    return;
                }
                isChecked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? CheckedChanged;

        protected override void OnClick(EventArgs e)
        {
            if (CheckOnClick)
            {
                Checked = !Checked;
            }
            base.OnClick(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                pressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // The rounded shape leaves the corners of the control unpainted, so
            // clear the whole surface with the parent colour first.
            graphics.Clear(Parent != null ? Parent.BackColor : Brand.Paper);

            Color back;
            Color border;
            Color text = Enabled ? Brand.Navy : Brand.DisabledText;

            if (!Enabled)
            {
                back = style == BrandButtonStyle.Primary
                    ? Color.FromArgb(196, 209, 219)
                    : Color.FromArgb(246, 249, 251);
                border = Color.FromArgb(219, 230, 238);
                text = Color.White;
            }
            else if (isChecked)
            {
                back = pressed ? Brand.PaleBlueHover : Brand.PaleBlue;
                border = Brand.Accent;
                text = Brand.Navy;
            }
            else if (style == BrandButtonStyle.Primary)
            {
                back = pressed ? Brand.NavyPressed : (hovered ? Brand.NavyHover : Brand.Navy);
                border = back;
                text = Color.White;
            }
            else
            {
                back = pressed ? Brand.PaleBlueHover : (hovered ? Brand.PaleBlue : Brand.Paper);
                border = hovered ? Brand.Accent : Brand.PaleBorder;
                text = Brand.Navy;
            }

            if (!Enabled && style != BrandButtonStyle.Primary)
            {
                text = Brand.DisabledText;
            }

            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (GraphicsPath path = RoundedRectangle(bounds, 5f))
            {
                using (var brush = new SolidBrush(back))
                {
                    graphics.FillPath(brush, path);
                }
                using var pen = new Pen(border, 1f);
                graphics.DrawPath(pen, path);
            }

            var textBounds = new Rectangle(2, 0, Width - 4, Height);
            TextRenderer.DrawText(
                graphics,
                Text,
                Font,
                textBounds,
                text,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding |
                TextFormatFlags.NoPrefix);
        }

        private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2f;

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180f, 90f);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270f, 90f);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0f, 90f);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90f, 90f);
            path.CloseFigure();
            return path;
        }
    }
}
