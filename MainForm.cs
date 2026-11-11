using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SimplePdfReader
{
    public partial class MainForm : Form
    {
        private WebView2 webView;
        private StatusStrip statusStrip;
        private ToolStrip toolStrip;
        private ToolStripButton openPdfButton;
        private ToolStripButton zoomInButton;
        private ToolStripButton zoomOutButton;
        private ToolStripButton prevPageButton;
        private ToolStripButton nextPageButton;
        private ToolStripButton continuousModeButton;
        private ToolStripStatusLabel pageStatusLabel;
        private ToolStripStatusLabel zoomStatusLabel;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.toolStrip = new ToolStrip();
            this.openPdfButton = new ToolStripButton();
            this.zoomInButton = new ToolStripButton();
            this.zoomOutButton = new ToolStripButton();
            this.prevPageButton = new ToolStripButton();
            this.nextPageButton = new ToolStripButton();
            this.continuousModeButton = new ToolStripButton();
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
            this.zoomInButton,
            this.zoomOutButton,
            this.prevPageButton,
            this.nextPageButton,
            this.continuousModeButton});
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
            this.webView.CoreWebView2InitializationCompleted += new EventHandler<CoreWebView2InitializationCompletedEventArgs>(this.WebView_CoreWebView2InitializationCompleted);
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

        private async void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                webView.CoreWebView2.Navigate("https://appassets.example");
                webView.CoreWebView2.Environment.SetVirtualHostNameToFolderMapping("appassets.example", AppContext.BaseDirectory, CoreWebView2HostResourceAccessKind.Allow);
                webView.CoreWebView2.Environment.SetVirtualHostNameToFolderMapping("localpdf.example", Path.GetDirectoryName(Application.ExecutablePath), CoreWebView2HostResourceAccessKind.Allow);
            }
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var message = e.TryGetWebMessageAsString();
            if (message != null)
            {
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(message);
                pageStatusLabel.Text = $"Page {data.page} of {data.total}";
                zoomStatusLabel.Text = $"Zoom: {data.zoom}%";
            }
        }

        private void OpenPdfButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PDF Files|*.pdf";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    webView.CoreWebView2.ExecuteScriptAsync($"loadPdf('https://localpdf.example{Uri.EscapeDataString(openFileDialog.FileName)}')");
                }
            }
        }

        private void ZoomInButton_Click(object sender, EventArgs e)
        {
            webView.CoreWebView2.ExecuteScriptAsync("zoomIn()");
        }

        private void ZoomOutButton_Click(object sender, EventArgs e)
        {
            webView.CoreWebView2.ExecuteScriptAsync("zoomOut()");
        }

        private void PrevPageButton_Click(object sender, EventArgs e)
        {
            webView.CoreWebView2.ExecuteScriptAsync("prevPage()");
        }

        private void NextPageButton_Click(object sender, EventArgs e)
        {
            webView.CoreWebView2.ExecuteScriptAsync("nextPage()");
        }

        private void ContinuousModeButton_CheckedChanged(object sender, EventArgs e)
        {
            webView.CoreWebView2.ExecuteScriptAsync($"setContinuousMode({continuousModeButton.Checked})");
        }
    }
}
