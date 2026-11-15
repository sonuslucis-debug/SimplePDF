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
        private Panel headerPanel = null!;
        private PictureBox headerLogo = null!;
        private Label titleLabel = null!;
        private Label subtitleLabel = null!;
        private Panel toolbarPanel = null!;
        private Panel statusPanel = null!;
        private BrandButton openPdfButton = null!;
        private BrandButton printButton = null!;
        private BrandButton zoomInButton = null!;
        private BrandButton zoomOutButton = null!;
        private BrandButton prevPageButton = null!;
        private BrandButton nextPageButton = null!;
        private BrandButton continuousModeButton = null!;
        private BrandButton aboutButton = null!;
        private Label pageStatusLabel = null!;
        private Label zoomStatusLabel = null!;
        private readonly ToolTip toolTip = new ToolTip();

        private readonly List<string> documentFiles = new();
        private string? webRoot;
        private string? pendingFilePath;
        private string? lastDropPath;
        private DateTime lastDropUtc;
        private bool viewerReady;
        private int documentCounter;
        private int lastPage;
        private int lastPageTotal;

        internal static Icon? AppIcon => appIcon;

        public MainForm(string? initialFile = null)
        {
            InitializeComponent();

            pendingFilePath = initialFile;

            appIcon = LoadAppIcon();
            if (appIcon != null)
            {
                this.Icon = appIcon;
            }

            this.FormClosed += MainForm_FormClosed;

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

                string webRootPath = PrepareWebAssets(userDataFolder);
                webRoot = webRootPath;
                DeleteStaleDocumentFiles();

                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "appassets.example",
                    webRootPath,
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

        private async void CoreWebView2_NavigationCompleted(
            object? sender,
            CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                return;
            }

            string? source = webView.CoreWebView2?.Source;
            if (source == null ||
                !source.StartsWith("https://appassets.example/viewer.html", StringComparison.Ordinal))
            {
                return;
            }

            viewerReady = true;

            if (pendingFilePath != null)
            {
                string filePath = pendingFilePath;
                pendingFilePath = null;
                await OpenPdfAsync(filePath);
            }
        }

        private async void CoreWebView2_NewWindowRequested(
            object? sender,
            CoreWebView2NewWindowRequestedEventArgs e)
        {
            // The app never needs a second browser window.
            e.Handled = true;

            string uri = e.Uri;
            if (string.IsNullOrEmpty(uri) ||
                !Uri.TryCreate(uri, UriKind.Absolute, out Uri? parsed) ||
                !parsed.IsFile)
            {
                return;
            }

            string filePath = parsed.LocalPath;
            if (IsPdfFile(filePath))
            {
                await OpenPdfAsync(filePath);
            }
        }

        private async Task OpenPdfAsync(string filePath)
        {
            if (IsDisposed)
            {
                return;
            }

            if (!viewerReady || string.IsNullOrEmpty(webRoot) || webView.CoreWebView2 == null)
            {
                pendingFilePath = filePath;
                return;
            }

            if (!File.Exists(filePath))
            {
                MessageBox.Show(
                    this,
                    "The selected PDF file no longer exists.",
                    "Simple PDF Reader",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string documentName = $"document-{Environment.ProcessId}-{++documentCounter}.pdf";
            string destinationPath = Path.Combine(webRoot!, documentName);
            string temporaryPath = destinationPath + ".tmp";

            pageStatusLabel.Text = "Loading PDF…";
            printButton.Enabled = false;

            try
            {
                await using (var source = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan))
                await using (var destination = new FileStream(
                    temporaryPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    FileOptions.Asynchronous))
                {
                    await source.CopyToAsync(destination);
                }

                File.Move(temporaryPath, destinationPath, true);
                documentFiles.Add(destinationPath);

                if (IsDisposed)
                {
                    return;
                }

                // The viewer and the document share the same origin, so PDF.js can
                // fetch the document without cross-origin restrictions.
                string url = documentName + "?v=" + Environment.TickCount64;
                await webView.CoreWebView2.ExecuteScriptAsync($"loadPdf('{url}')");
                this.Text = $"Simple PDF Reader - {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                TryDeleteFile(temporaryPath);

                if (IsDisposed)
                {
                    return;
                }

                pageStatusLabel.Text = "Could not open the PDF file";
                printButton.Enabled = false;
                MessageBox.Show(
                    this,
                    $"The PDF could not be opened.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                    "Simple PDF Reader",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void DeleteStaleDocumentFiles()
        {
            if (string.IsNullOrEmpty(webRoot))
            {
                return;
            }

            try
            {
                foreach (string file in Directory.EnumerateFiles(webRoot!, "document-*.pdf*"))
                {
                    if (!IsDocumentFileInUseByAnotherInstance(file))
                    {
                        TryDeleteFile(file);
                    }
                }
            }
            catch
            {
                // Ignore cleanup failures; a new asset folder is used after updates.
            }
        }

        private static bool IsDocumentFileInUseByAnotherInstance(string path)
        {
            string[] parts = Path.GetFileName(path).Split('-');
            if (parts.Length < 3 || !int.TryParse(parts[1], out int ownerProcessId))
            {
                return false;
            }

            if (ownerProcessId == Environment.ProcessId)
            {
                // Left over from an earlier run that reused this process id.
                return false;
            }

            try
            {
                using var process = System.Diagnostics.Process.GetProcessById(ownerProcessId);
                return process.ProcessName.Equals(
                    "SimplePdfReader",
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            foreach (string file in documentFiles)
            {
                TryDeleteFile(file);
            }

            documentFiles.Clear();
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Ignore files that are still in use.
            }
        }

        private void HandleDragEnter(object? sender, DragEventArgs e)
        {
            string[]? files = e.Data?.GetData(DataFormats.FileDrop) as string[];
            e.Effect = files != null && files.Any(IsPdfFile)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private async void HandleDragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files)
            {
                return;
            }

            string? pdfPath = files.FirstOrDefault(IsPdfFile);
            if (pdfPath == null)
            {
                return;
            }

            // Both the form and the WebView can report the same drop.
            if (pdfPath == lastDropPath &&
                (DateTime.UtcNow - lastDropUtc).TotalMilliseconds < 1000)
            {
                return;
            }

            lastDropPath = pdfPath;
            lastDropUtc = DateTime.UtcNow;
            await OpenPdfAsync(pdfPath);
        }

        private static bool IsPdfFile(string path)
        {
            return path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
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
                "style.css" => "style.css",
                "pdfjs.pdf.min.js" => "pdfjs/pdf.min.js",
                "pdfjs.pdf.worker.min.js" => "pdfjs/pdf.worker.min.js",
                "pdfjs.LICENSE" => "pdfjs/LICENSE",
                _ => null
            };
        }

        private void InitializeComponent()
        {
            this.headerPanel = new Panel();
            this.headerLogo = new PictureBox();
            this.titleLabel = new Label();
            this.subtitleLabel = new Label();
            this.aboutButton = new BrandButton();
            this.toolbarPanel = new Panel();
            this.openPdfButton = new BrandButton();
            this.printButton = new BrandButton();
            this.prevPageButton = new BrandButton();
            this.nextPageButton = new BrandButton();
            this.continuousModeButton = new BrandButton();
            this.zoomOutButton = new BrandButton();
            this.zoomInButton = new BrandButton();
            this.statusPanel = new Panel();
            this.pageStatusLabel = new Label();
            this.zoomStatusLabel = new Label();
            this.webView = new WebView2();
            ((System.ComponentModel.ISupportInitialize)this.headerLogo).BeginInit();
            this.headerPanel.SuspendLayout();
            this.toolbarPanel.SuspendLayout();
            this.statusPanel.SuspendLayout();
            this.SuspendLayout();
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(this.HandleDragEnter);
            this.DragDrop += new DragEventHandler(this.HandleDragDrop);
            // 
            // headerPanel
            // 
            this.headerPanel.BackColor = Brand.Navy;
            this.headerPanel.Dock = DockStyle.Top;
            this.headerPanel.Height = 58;
            this.headerPanel.Name = "headerPanel";
            // 
            // headerLogo
            // 
            this.headerLogo.BackColor = Color.Transparent;
            this.headerLogo.Image = Brand.LoadLogo();
            this.headerLogo.Location = new Point(16, 11);
            this.headerLogo.Name = "headerLogo";
            this.headerLogo.Size = new Size(36, 36);
            this.headerLogo.SizeMode = PictureBoxSizeMode.Zoom;
            this.headerLogo.TabStop = false;
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.BackColor = Color.Transparent;
            this.titleLabel.Font = Brand.Font(13.5f, FontStyle.Bold);
            this.titleLabel.ForeColor = Color.White;
            this.titleLabel.Location = new Point(60, 8);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Text = "SIMPLE PDF";
            // 
            // subtitleLabel
            // 
            this.subtitleLabel.AutoSize = true;
            this.subtitleLabel.BackColor = Color.Transparent;
            this.subtitleLabel.Font = Brand.Font(8.25f, FontStyle.Regular);
            this.subtitleLabel.ForeColor = Brand.OnNavyMuted;
            this.subtitleLabel.Location = new Point(62, 32);
            this.subtitleLabel.Name = "subtitleLabel";
            this.subtitleLabel.Text = "by Sonus Lucis";
            // 
            // aboutButton
            // 
            this.aboutButton.Margin = new Padding(0);
            this.aboutButton.Name = "aboutButton";
            this.aboutButton.Size = new Size(84, 30);
            this.aboutButton.Style = BrandButtonStyle.Header;
            this.aboutButton.Text = "About";
            this.aboutButton.Click += new EventHandler(this.AboutButton_Click);
            // 
            // headerActions
            // 
            var headerActions = new FlowLayoutPanel();
            headerActions.BackColor = Brand.Navy;
            headerActions.Dock = DockStyle.Right;
            headerActions.FlowDirection = FlowDirection.RightToLeft;
            headerActions.Padding = new Padding(12, 14, 16, 0);
            headerActions.Width = 112;
            headerActions.WrapContents = false;
            headerActions.Controls.Add(this.aboutButton);
            this.headerPanel.Controls.Add(this.headerLogo);
            this.headerPanel.Controls.Add(this.titleLabel);
            this.headerPanel.Controls.Add(this.subtitleLabel);
            this.headerPanel.Controls.Add(headerActions);
            // 
            // toolbarPanel
            // 
            this.toolbarPanel.BackColor = Brand.Paper;
            this.toolbarPanel.Dock = DockStyle.Top;
            this.toolbarPanel.Height = 48;
            this.toolbarPanel.Name = "toolbarPanel";
            this.toolbarPanel.Paint += new PaintEventHandler(this.ToolbarPanel_Paint);
            // 
            // openPdfButton
            // 
            this.openPdfButton.Location = new Point(16, 9);
            this.openPdfButton.Name = "openPdfButton";
            this.openPdfButton.Size = new Size(100, 30);
            this.openPdfButton.Style = BrandButtonStyle.Primary;
            this.openPdfButton.Text = "Open PDF";
            this.openPdfButton.Click += new EventHandler(this.OpenPdfButton_Click);
            // 
            // printButton
            // 
            this.printButton.Enabled = false;
            this.printButton.Location = new Point(124, 9);
            this.printButton.Name = "printButton";
            this.printButton.Size = new Size(72, 30);
            this.printButton.Text = "Print";
            this.printButton.Click += new EventHandler(this.PrintButton_Click);
            this.toolTip.SetToolTip(this.printButton, "Print the open document (Ctrl+P)");
            // 
            // prevPageButton
            // 
            this.prevPageButton.Location = new Point(216, 9);
            this.prevPageButton.Name = "prevPageButton";
            this.prevPageButton.Size = new Size(82, 30);
            this.prevPageButton.Text = "Prev Page";
            this.prevPageButton.Click += new EventHandler(this.PrevPageButton_Click);
            // 
            // nextPageButton
            // 
            this.nextPageButton.Location = new Point(306, 9);
            this.nextPageButton.Name = "nextPageButton";
            this.nextPageButton.Size = new Size(82, 30);
            this.nextPageButton.Text = "Next Page";
            this.nextPageButton.Click += new EventHandler(this.NextPageButton_Click);
            // 
            // continuousModeButton
            // 
            this.continuousModeButton.CheckOnClick = true;
            this.continuousModeButton.Location = new Point(408, 9);
            this.continuousModeButton.Name = "continuousModeButton";
            this.continuousModeButton.Size = new Size(118, 30);
            this.continuousModeButton.Text = "Mode: Page";
            this.continuousModeButton.CheckedChanged += new EventHandler(this.ContinuousModeButton_CheckedChanged);
            // 
            // zoomOutButton
            // 
            this.zoomOutButton.Location = new Point(546, 9);
            this.zoomOutButton.Name = "zoomOutButton";
            this.zoomOutButton.Size = new Size(36, 30);
            this.zoomOutButton.Text = "-";
            this.zoomOutButton.Click += new EventHandler(this.ZoomOutButton_Click);
            this.toolTip.SetToolTip(this.zoomOutButton, "Zoom out (Ctrl+scroll down)");
            // 
            // zoomInButton
            // 
            this.zoomInButton.Location = new Point(590, 9);
            this.zoomInButton.Name = "zoomInButton";
            this.zoomInButton.Size = new Size(36, 30);
            this.zoomInButton.Text = "+";
            this.zoomInButton.Click += new EventHandler(this.ZoomInButton_Click);
            this.toolTip.SetToolTip(this.zoomInButton, "Zoom in (Ctrl+scroll up)");
            // 
            // toolbarSeparator1
            // 
            var toolbarSeparator1 = new Panel();
            toolbarSeparator1.BackColor = Brand.PaleBorder;
            toolbarSeparator1.Location = new Point(204, 14);
            toolbarSeparator1.Size = new Size(1, 20);
            // 
            // toolbarSeparator2
            // 
            var toolbarSeparator2 = new Panel();
            toolbarSeparator2.BackColor = Brand.PaleBorder;
            toolbarSeparator2.Location = new Point(396, 14);
            toolbarSeparator2.Size = new Size(1, 20);
            // 
            // toolbarSeparator3
            // 
            var toolbarSeparator3 = new Panel();
            toolbarSeparator3.BackColor = Brand.PaleBorder;
            toolbarSeparator3.Location = new Point(534, 14);
            toolbarSeparator3.Size = new Size(1, 20);
            this.toolbarPanel.Controls.Add(this.openPdfButton);
            this.toolbarPanel.Controls.Add(this.printButton);
            this.toolbarPanel.Controls.Add(toolbarSeparator1);
            this.toolbarPanel.Controls.Add(this.prevPageButton);
            this.toolbarPanel.Controls.Add(this.nextPageButton);
            this.toolbarPanel.Controls.Add(toolbarSeparator2);
            this.toolbarPanel.Controls.Add(this.continuousModeButton);
            this.toolbarPanel.Controls.Add(toolbarSeparator3);
            this.toolbarPanel.Controls.Add(this.zoomOutButton);
            this.toolbarPanel.Controls.Add(this.zoomInButton);
            // 
            // statusPanel
            // 
            this.statusPanel.BackColor = Brand.PaleBlue;
            this.statusPanel.Dock = DockStyle.Bottom;
            this.statusPanel.Height = 28;
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Padding = new Padding(16, 0, 16, 0);
            this.statusPanel.Paint += new PaintEventHandler(this.StatusPanel_Paint);
            // 
            // pageStatusLabel
            // 
            this.pageStatusLabel.AutoSize = false;
            this.pageStatusLabel.BackColor = Color.Transparent;
            this.pageStatusLabel.Dock = DockStyle.Fill;
            this.pageStatusLabel.Font = Brand.Font(9f, FontStyle.Regular);
            this.pageStatusLabel.ForeColor = Brand.Navy;
            this.pageStatusLabel.Name = "pageStatusLabel";
            this.pageStatusLabel.Text = "No PDF loaded";
            this.pageStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // zoomStatusLabel
            // 
            this.zoomStatusLabel.AutoSize = false;
            this.zoomStatusLabel.BackColor = Color.Transparent;
            this.zoomStatusLabel.Dock = DockStyle.Right;
            this.zoomStatusLabel.Font = Brand.Font(9f, FontStyle.Bold);
            this.zoomStatusLabel.ForeColor = Brand.Accent;
            this.zoomStatusLabel.Name = "zoomStatusLabel";
            this.zoomStatusLabel.Size = new Size(120, 28);
            this.zoomStatusLabel.Text = "Zoom: 100%";
            this.zoomStatusLabel.TextAlign = ContentAlignment.MiddleRight;
            this.statusPanel.Controls.Add(this.pageStatusLabel);
            this.statusPanel.Controls.Add(this.zoomStatusLabel);
            // 
            // webView
            // 
            this.webView.AllowExternalDrop = true;
            this.webView.DefaultBackgroundColor = Brand.Navy;
            this.webView.Dock = DockStyle.Fill;
            this.webView.Location = new Point(0, 106);
            this.webView.Name = "webView";
            this.webView.Size = new System.Drawing.Size(1024, 586);
            this.webView.TabIndex = 2;
            this.webView.DragEnter += new DragEventHandler(this.HandleDragEnter);
            this.webView.DragDrop += new DragEventHandler(this.HandleDragDrop);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Brand.Navy;
            this.ClientSize = new System.Drawing.Size(1024, 720);
            this.MinimumSize = new System.Drawing.Size(760, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Controls.Add(this.webView);
            this.Controls.Add(this.statusPanel);
            this.Controls.Add(this.toolbarPanel);
            this.Controls.Add(this.headerPanel);
            this.Name = "MainForm";
            this.Text = "Simple PDF Reader";
            ((System.ComponentModel.ISupportInitialize)this.headerLogo).EndInit();
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            this.toolbarPanel.ResumeLayout(false);
            this.statusPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void ToolbarPanel_Paint(object? sender, PaintEventArgs e)
        {
            using var pen = new Pen(Brand.PaleBorder);
            e.Graphics.DrawLine(pen, 0, toolbarPanel.Height - 1, toolbarPanel.Width, toolbarPanel.Height - 1);
        }

        private void StatusPanel_Paint(object? sender, PaintEventArgs e)
        {
            using var pen = new Pen(Brand.PaleBorder);
            e.Graphics.DrawLine(pen, 0, 0, statusPanel.Width, 0);
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

                        lastPage = page;
                        lastPageTotal = total;
                        pageStatusLabel.Text = FormatPageStatus();
                        zoomStatusLabel.Text = $"Zoom: {Math.Round(zoom * 100)}%";
                        printButton.Enabled = total > 0;
                        break;

                    case "document":
                        if (root.TryGetProperty("name", out JsonElement nameElement))
                        {
                            string? name = nameElement.GetString();
                            if (!string.IsNullOrEmpty(name))
                            {
                                this.Text = $"Simple PDF Reader - {name}";
                            }
                        }
                        break;

                    case "status":
                        if (root.TryGetProperty("text", out JsonElement textElement))
                        {
                            string? text = textElement.GetString();
                            if (!string.IsNullOrEmpty(text))
                            {
                                // "Ready" is a transient message; keep the page
                                // position visible in the status bar instead.
                                pageStatusLabel.Text = string.Equals(text, "Ready", StringComparison.Ordinal)
                                    ? FormatPageStatus()
                                    : text;
                            }
                        }
                        break;
                }
            }
            catch (JsonException)
            {
                // Ignore malformed messages from the page.
            }
        }

        private string FormatPageStatus()
        {
            return lastPageTotal > 0 ? $"Page {lastPage} of {lastPageTotal}" : "No PDF loaded";
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
                    await OpenPdfAsync(openFileDialog.FileName);
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
            continuousModeButton.Text = continuousModeButton.Checked
                ? "Mode: Continuous"
                : "Mode: Page";

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
