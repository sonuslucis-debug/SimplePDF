using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SimplePdfReader
{
    public sealed class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "About Simple PDF Reader";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(480, 400);
            this.BackColor = Brand.Paper;
            this.Icon = MainForm.AppIcon;

            var logoBox = new PictureBox
            {
                Location = new Point(24, 24),
                Size = new Size(96, 96),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = LoadLogo()
            };

            var titleLabel = new Label
            {
                AutoSize = true,
                Location = new Point(140, 26),
                Font = Brand.Font(16F, FontStyle.Bold),
                ForeColor = Brand.Navy,
                Text = "Simple PDF Reader"
            };

            var versionLabel = new Label
            {
                AutoSize = true,
                Location = new Point(143, 64),
                ForeColor = Color.FromArgb(120, 141, 158),
                Text = $"Version {Application.ProductVersion}"
            };

            var descriptionLabel = new Label
            {
                Location = new Point(140, 92),
                Size = new Size(316, 42),
                ForeColor = Brand.Ink,
                Text = "A simple PDF reader with no bells and whistles."
            };

            var separator = new Panel
            {
                Location = new Point(24, 144),
                Size = new Size(432, 1),
                BackColor = Brand.PaleBorder
            };

            var creditsLabel = new Label
            {
                Location = new Point(24, 158),
                Size = new Size(432, 132),
                ForeColor = Brand.Ink,
                Text =
                    "PDF rendering by Mozilla PDF.js.\r\n" +
                    "Built with .NET 8, Windows Forms and WebView2.\r\n\r\n" +
                    "Design and branding by Sonus Lucis.\r\n\r\n" +
                    "Printing support, application icon and this About dialog by " +
                    "DeepSeek V4.1 Flash Free (OpenCode)."
            };

            var websiteLink = new LinkLabel
            {
                AutoSize = true,
                Location = new Point(24, 302),
                Text = "sonuslucis.com",
                LinkColor = Brand.Accent,
                ActiveLinkColor = Brand.Navy
            };
            websiteLink.LinkClicked += (_, _) => OpenWebsite("https://sonuslucis.com");

            var okButton = new BrandButton
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(372, 352),
                Size = new Size(84, 30),
                Style = BrandButtonStyle.Primary
            };

            this.Controls.AddRange(new Control[]
            {
                logoBox,
                titleLabel,
                versionLabel,
                descriptionLabel,
                separator,
                creditsLabel,
                websiteLink,
                okButton
            });

            this.AcceptButton = okButton;
            this.CancelButton = okButton;
        }

        private static void OpenWebsite(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                // Ignore failures if no default browser is available.
            }
        }

        private static Image? LoadLogo()
        {
            try
            {
                using Stream? stream = typeof(AboutForm).Assembly
                    .GetManifestResourceStream("SimplePdfReader.logo.png");
                if (stream == null)
                {
                    return null;
                }

                using var image = Image.FromStream(stream);
                return new Bitmap(image);
            }
            catch
            {
                return null;
            }
        }
    }
}
