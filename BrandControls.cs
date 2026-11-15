using System;
using System.Drawing;
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
        Outline,
        Header
    }

    // Flat button that follows the navy/accent palette instead of the system
    // chrome. It uses the native flat renderer so text and edges stay crisp at
    // any display scaling.
    internal sealed class BrandButton : Button
    {
        private bool isChecked;
        private BrandButtonStyle style = BrandButtonStyle.Outline;

        public BrandButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 1;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;
            Font = Brand.Font(9f, FontStyle.Bold);
            Height = 30;
            ApplyColors();
        }

        public BrandButtonStyle Style
        {
            get => style;
            set
            {
                style = value;
                ApplyColors();
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
                ApplyColors();
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

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            ApplyColors();
        }

        private void ApplyColors()
        {
            Color back;
            Color fore;
            Color border;
            Color hover;
            Color pressed;

            if (isChecked)
            {
                back = Brand.Accent;
                fore = Color.White;
                border = Brand.Accent;
                hover = Brand.Navy;
                pressed = Brand.NavyPressed;
            }
            else
            {
                switch (style)
                {
                    case BrandButtonStyle.Primary:
                        back = Brand.Navy;
                        fore = Color.White;
                        border = Brand.Navy;
                        hover = Brand.NavyHover;
                        pressed = Brand.NavyPressed;
                        break;

                    case BrandButtonStyle.Header:
                        back = Brand.Navy;
                        fore = Color.White;
                        border = Brand.OnNavyMuted;
                        hover = Brand.Accent;
                        pressed = Brand.NavyPressed;
                        break;

                    default:
                        back = Brand.Paper;
                        fore = Brand.Navy;
                        border = Brand.PaleBorder;
                        hover = Brand.PaleBlue;
                        pressed = Brand.PaleBlueHover;
                        break;
                }
            }

            if (!Enabled && !isChecked)
            {
                back = style == BrandButtonStyle.Primary
                    ? Color.FromArgb(190, 204, 214)
                    : (style == BrandButtonStyle.Header ? Brand.Navy : Brand.Paper);
                fore = style == BrandButtonStyle.Header ? Brand.OnNavyMuted : Brand.DisabledText;
                border = style == BrandButtonStyle.Header ? Brand.Navy : Brand.PaleBorder;
                hover = back;
                pressed = back;
            }

            BackColor = back;
            ForeColor = fore;
            FlatAppearance.BorderColor = border;
            FlatAppearance.MouseOverBackColor = hover;
            FlatAppearance.MouseDownBackColor = pressed;
        }
    }
}