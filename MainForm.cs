using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SimplePdfReader
{
    public partial class MainForm : Form
    {
        private const string WebAssetResourcePrefix = "SimplePdfReader.web.";

        private static Icon? appIcon;
        private static Stream? appIconStream;

        private WebView2 webView = null!;
        private StatusStrip statusStrip = null!;
        private ToolStrip toolStrip = null!;
        private ToolStripButton openPdfButton = null!;
        private ToolStripButton printButton = null!;
        private ToolStripButton zoomInButton = null!;
        private ToolStripButton zoomOutButton = null!;
        private ToolStripButton prevPageButton = null!;
        private ToolStripButton nextPageButton = null!;
        private ToolStripButton continuousModeButton = null!;
        private ToolStripButton aboutButton = null!;
        private ToolStripStatusLabel pageStatusLabel = null!;
        private ToolStripStatusLabel zoomStatusLabel = null!;

        internal static Icon? AppIcon => appIcon;

        public MainForm()
        {
            InitializeComponent();

            appIcon = LoadAppIcon();
            if (appIcon != null)
            {
                this.Icon = appIcon;
            }

            _ = InitializeWebViewAsync();
        }

        private static Icon? LoadAppIcon()
        {
            try
            {
                // Keep the stream alive for as long as the icon is in use.
                appIconStream = typeof(MainForm).Assembly.GetManifestResourceStream("SimplePdfReader.app.ico");
                if (appIconStream != null)
                {
                    return new Icon(appIconStream);
                }
            }
            catch
            {
                // Fall back to the icon embedded in the executable.
            }

            try
            {
                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                return null;
            }
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SimplePdfReader");
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                string webRoot = PrepareWebAssets(userDataFolder);

                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "appassets.example",
                    webRoot,
                    CoreWebView2HostResourceAccessKind.Allow);
                webView.CoreWebView2.Navigate("https://appassets.example/viewer.html");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"The PDF viewer could not be started.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                    "Simple PDF Reader",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static string PrepareWebAssets(string userDataFolder)
        {
            string assetHash = ComputeWebAssetHash();
            string webRoot = Path.Combine(userDataFolder, "web", assetHash);
            string markerPath = Path.Combine(webRoot, ".ready");

            if (File.Exists(markerPath))
            {
                return webRoot;
            }

            string tempRoot = webRoot + ".tmp-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(tempRoot);

            try
            {
                var assembly = typeof(MainForm).Assembly;
                foreach (string resourceName in assembly.GetManifestResourceNames())
                {
                    if (!resourceName.StartsWith(WebAssetResourcePrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string? relativePath = GetWebAssetPath(
                        resourceName.Substring(WebAssetResourcePrefix.Length));
                    if (relativePath == null)
                    {
                        continue;
                    }

                    string destinationPath = Path.Combine(
                        tempRoot,
                        relativePath.Replace('/', Path.DirectorySeparatorChar));
                    string? destinationFolder = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destinationFolder))
                    {
                        Directory.CreateDirectory(destinationFolder);
                    }

                    using Stream source = assembly.GetManifestResourceStream(resourceName)
                        ?? throw new InvalidOperationException($"Missing embedded resource: {resourceName}");
                    using FileStream destination = File.Create(destinationPath);
                    source.CopyTo(destination);
                }

                File.WriteAllText(Path.Combine(tempRoot, ".ready"), assetHash);

                if (Directory.Exists(webRoot))
                {
                    Directory.Delete(webRoot, true);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(webRoot)!);
                Directory.Move(tempRoot, webRoot);
            }
            catch
            {
                if (Directory.Exists(tempRoot))
                {
                    try
                    {
                        Directory.Delete(tempRoot, true);
                    }
                    catch
                    {
                        // Ignore cleanup failures.
                    }
                }

                throw;
            }

            return webRoot;
        }

        private static string ComputeWebAssetHash()
        {
            var assembly = typeof(MainForm).Assembly;
            string[] resourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(WebAssetResourcePrefix, StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            using var sha256 = SHA256.Create();
            byte[] buffer = new byte[81920];

            foreach (string resourceName in resourceNames)
            {
                byte[] nameBytes = Encoding.UTF8.GetBytes(resourceName);
                sha256.TransformBlock(nameBytes, 0, nameBytes.Length, null, 0);

                using Stream stream = assembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidOperationException($"Missing embedded resource: {resourceName}");
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
            }

            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return Convert.ToHexString(sha256.Hash!)
                .ToLowerInvariant()
                .Substring(0, 16);
        }

        private static string? GetWebAssetPath(string resourceSuffix)
        {
            if (resourceSuffix.StartsWith("pdfjs.cmaps.", StringComparison.Ordinal))
            {
                return "pdfjs/cmaps/" + resourceSuffix.Substring("pdfjs.cmaps.".Length);
            }

            if (resourceSuffix.StartsWith("pdfjs.standard_fonts.", StringComparison.Ordinal))
            {
                return "pdfjs/standard_fonts/" + resourceSuffix.Substring("pdfjs.standard_fonts.".Length);
            }

            return resourceSuffix switch
            {
                "viewer.html" => "viewer.html",
                "pdfjs.pdf.min.js" => "pdfjs/pdf.min.js",
                "pdfjs.pdf.worker.min.js" => "pdfjs/pdf.worker.min.js",
                "pdfjs.LICENSE" => "pdfjs/LICENSE",
                _ => null
            };
        }

        private void InitializeComponent()
        {
            this.toolStrip = new ToolStrip();
            this.openPdfButton = new ToolStripButton();
            this.printButton = new ToolStripButton();
            this.zoomInButton = new ToolStripButton();
            this.zoomOutButton = new ToolStripButton();
            this.prevPageButton = new ToolStripButton();
            this.nextPageButton = new ToolStripButton();
            this.continuousModeButton = new ToolStripButton();
            this.aboutButton = new ToolStripButton();
            this.statusStrip = new StatusStrip();
            this.pageStatusLabel = new ToolStripStatusLabel();
            this.zoomStatusLabel = new ToolStripStatusLabel();
            this.webView = new WebView2();
            this.toolStrip.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            this.toolStrip.Items.AddRange(new ToolStripItem[] {
            this.openPdfButton,
            this.printButton,
            this.zoomInButton,
            this.zoomOutButton,
            this.prevPageButton,
            this.nextPageButton,
            this.continuousModeButton,
            this.aboutButton});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(800, 25);
            this.toolStrip.TabIndex = 0;
            this.toolStrip.Text = "toolStrip";
            // 
            // openPdfButton
            // 
            this.openPdfButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.openPdfButton.Name = "openPdfButton";
            this.openPdfButton.Size = new System.Drawing.Size(75, 22);
            this.openPdfButton.Text = "Open PDF";
            this.openPdfButton.Click += new EventHandler(this.OpenPdfButton_Click);
            // 
            // printButton
            // 
            this.printButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.printButton.Enabled = false;
            this.printButton.Name = "printButton";
            this.printButton.Size = new System.Drawing.Size(50, 22);
            this.printButton.Text = "Print";
            this.printButton.ToolTipText = "Print the open document (Ctrl+P)";
            this.printButton.Click += new EventHandler(this.PrintButton_Click);
            // 
            // zoomInButton
            // 
            this.zoomInButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.zoomInButton.Name = "zoomInButton";
            this.zoomInButton.Size = new System.Drawing.Size(50, 22);
            this.zoomInButton.Text = "Zoom In (+)";
            this.zoomInButton.Click += new EventHandler(this.ZoomInButton_Click);
            // 
            // zoomOutButton
            // 
            this.zoomOutButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.zoomOutButton.Name = "zoomOutButton";
            this.zoomOutButton.Size = new System.Drawing.Size(54, 22);
            this.zoomOutButton.Text = "Zoom Out (-)";
            this.zoomOutButton.Click += new EventHandler(this.ZoomOutButton_Click);
            // 
            // prevPageButton
            // 
            this.prevPageButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.prevPageButton.Name = "prevPageButton";
            this.prevPageButton.Size = new System.Drawing.Size(50, 22);
            this.prevPageButton.Text = "Prev Page";
            this.prevPageButton.Click += new EventHandler(this.PrevPageButton_Click);
            // 
            // nextPageButton
            // 
            this.nextPageButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.nextPageButton.Name = "nextPageButton";
            this.nextPageButton.Size = new System.Drawing.Size(50, 22);
            this.nextPageButton.Text = "Next Page";
            this.nextPageButton.Click += new EventHandler(this.NextPageButton_Click);
            // 
            // continuousModeButton
            // 
            this.continuousModeButton.CheckOnClick = true;
            this.continuousModeButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.continuousModeButton.Name = "continuousModeButton";
            this.continuousModeButton.Size = new System.Drawing.Size(80, 22);
            this.continuousModeButton.Text = "Mode: Continuous";
            this.continuousModeButton.CheckedChanged += new EventHandler(this.ContinuousModeButton_CheckedChanged);
            // 
            // aboutButton
            // 
            this.aboutButton.Alignment = ToolStripItemAlignment.Right;
            this.aboutButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.aboutButton.Name = "aboutButton";
            this.aboutButton.Size = new System.Drawing.Size(50, 22);
            this.aboutButton.Text = "About";
            this.aboutButton.Click += new EventHandler(this.AboutButton_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new ToolStripItem[] {
            this.pageStatusLabel,
            this.zoomStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 425);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(800, 22);
            this.statusStrip.TabIndex = 1;
            this.statusStrip.Text = "statusStrip";
            // 
            // pageStatusLabel
            // 
            this.pageStatusLabel.Name = "pageStatusLabel";
            this.pageStatusLabel.Size = new System.Drawing.Size(118, 17);
            this.pageStatusLabel.Text = "No PDF loaded";
            // 
            // zoomStatusLabel
            // 
            this.zoomStatusLabel.Alignment = ToolStripItemAlignment.Right;
            this.zoomStatusLabel.Name = "zoomStatusLabel";
            this.zoomStatusLabel.Size = new System.Drawing.Size(70, 17);
            this.zoomStatusLabel.Text = "Zoom: 100%";
            // 
            // webView
            // 
            this.webView.Dock = DockStyle.Fill;
            this.webView.Location = new System.Drawing.Point(0, 25);
            this.webView.Name = "webView";
            this.webView.Size = new System.Drawing.Size(800, 399);
            this.webView.TabIndex = 2;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 447);
            this.Controls.Add(this.webView);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.toolStrip);
            this.Name = "MainForm";
            this.Text = "Simple PDF Reader";
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(e.WebMessageAsJson);
                JsonElement root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("type", out JsonElement typeElement))
                {
                    return;
                }

                switch (typeElement.GetString())
                {
                    case "state":
                        int page = root.TryGetProperty("page", out JsonElement pageElement)
                            ? pageElement.GetInt32()
                            : 0;
                        int total = root.TryGetProperty("total", out JsonElement totalElement)
                            ? totalElement.GetInt32()
                            : 0;
                        double zoom = root.TryGetProperty("zoom", out JsonElement zoomElement)
                            ? zoomElement.GetDouble()
                            : 1.0;

                        pageStatusLabel.Text = total > 0 ? $"Page {page} of {total}" : "No PDF loaded";
                        zoomStatusLabel.Text = $"Zoom: {Math.Round(zoom * 100)}%";
                        printButton.Enabled = total > 0;
                        break;

                    case "status":
                        if (root.TryGetProperty("text", out JsonElement textElement))
                        {
                            pageStatusLabel.Text = textElement.GetString() ?? pageStatusLabel.Text;
                        }
                        break;
                }
            }
            catch (JsonException)
            {
                // Ignore malformed messages from the page.
            }
        }

        private async void OpenPdfButton_Click(object? sender, EventArgs e)
        {
            if (webView.CoreWebView2 == null) return;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PDF Files|*.pdf";
                openFileDialog.Title = "Open PDF";
                if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    string? folder = Path.GetDirectoryName(openFileDialog.FileName);
                    if (string.IsNullOrEmpty(folder)) return;

                    // Map the folder that contains the selected file so the WebView can load it.
                    webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "localpdf.example",
                        folder,
                        CoreWebView2HostResourceAccessKind.Allow);

                    string fileName = Uri.EscapeDataString(Path.GetFileName(openFileDialog.FileName));
                    string url = $"https://localpdf.example/{fileName}";

                    pageStatusLabel.Text = "Loading PDF…";
                    printButton.Enabled = false;
                    await webView.CoreWebView2.ExecuteScriptAsync($"loadPdf('{url}')");
                }
            }
        }

        private async void PrintButton_Click(object? sender, EventArgs e)
        {
            if (webView.CoreWebView2 == null) return;
            await webView.CoreWebView2.ExecuteScriptAsync("printPdf()");
        }

        private async void ZoomInButton_Click(object? sender, EventArgs e)
        {
            if (webView.CoreWebView2 == null) return;
            await webView.CoreWebView2.ExecuteScriptAsync("zoomIn()");
        }

        private async void ZoomOutButton_Click(object? sender, EventArgs e)
        {
            if (webView.CoreWebView2 == null) return;
            await webView.CoreWebView2.ExecuteScriptAsync("zoomOut()");
        }

        private async void PrevPageButton_Click(object? sender, EventArgs e)
        {
            if (webView.CoreWebView2 == null) return;
            await webView.CoreWebView2.ExecuteScriptAsync("prevPage()");
        }

        private async void NextPageButton_Click(object? sender, EventArgs e)
        {
            if (webView.CoreWebView2 == null) return;
            await webView.CoreWebView2.ExecuteScriptAsync("nextPage()");
        }

        private async void ContinuousModeButton_CheckedChanged(object? sender, EventArgs e)
        {
            if (webView.CoreWebView2 == null) return;
            await webView.CoreWebView2.ExecuteScriptAsync(
                $"setContinuousMode({(continuousModeButton.Checked ? "true" : "false")})");
        }

        private void AboutButton_Click(object? sender, EventArgs e)
        {
            using (var aboutForm = new AboutForm())
            {
                aboutForm.ShowDialog(this);
            }
        }
    }
}
