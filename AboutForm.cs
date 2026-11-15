using System;
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
            this.ClientSize = new Size(480, 360);
            this.BackColor = Color.White;
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
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Text = "Simple PDF Reader"
            };

            var versionLabel = new Label
            {
                AutoSize = true,
                Location = new Point(143, 64),
                ForeColor = Color.DimGray,
                Text = $"Version {Application.ProductVersion}"
            };

            var descriptionLabel = new Label
            {
                Location = new Point(140, 92),
                Size = new Size(316, 42),
                Text = "A simple PDF reader with no bells and whistles."
            };

            var separator = new Panel
            {
                Location = new Point(24, 144),
                Size = new Size(432, 1),
                BackColor = Color.Gainsboro
            };

            var creditsLabel = new Label
            {
                Location = new Point(24, 158),
                Size = new Size(432, 124),
                Text =
                    "PDF rendering by Mozilla PDF.js.\r\n" +
                    "Built with .NET 8, Windows Forms and WebView2.\r\n\r\n" +
                    "Printing support, application icon and this About dialog by " +
                    "DeepSeek V4.1 Flash Free (OpenCode)."
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(380, 310),
                Size = new Size(76, 28)
            };

            this.Controls.AddRange(new Control[]
            {
                logoBox,
                titleLabel,
                versionLabel,
                descriptionLabel,
                separator,
                creditsLabel,
                okButton
            });

            this.AcceptButton = okButton;
            this.CancelButton = okButton;
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
