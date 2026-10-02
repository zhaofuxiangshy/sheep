using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;

namespace WinFramework
{
    public class MainForm : Form
    {
        private TextBox txtUrl;
        private Button btnFetch;
        private TextBox txtStatus;
        private TextBox txtTitle;
        private TextBox txtMeta;
        private TextBox txtSummary;
        private Button btnExportJson;
        private Button btnCopy;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "WinFramework - Web Assistant";
            this.Width = 800;
            this.Height = 600;

            var lblUrl = new Label() { Text = "URL:", Left = 10, Top = 15, Width = 40 };
            txtUrl = new TextBox() { Left = 60, Top = 10, Width = 540 };
            btnFetch = new Button() { Text = "Fetch", Left = 610, Top = 8, Width = 80 };
            btnFetch.Click += async (s, e) => await FetchAndAnalyzeAsync();

            var lblStatus = new Label() { Text = "Status:", Left = 10, Top = 50, Width = 60 };
            txtStatus = new TextBox() { Left = 80, Top = 46, Width = 200, ReadOnly = true };

            var lblTitle = new Label() { Text = "Title:", Left = 10, Top = 80, Width = 60 };
            txtTitle = new TextBox() { Left = 80, Top = 76, Width = 690, ReadOnly = true };

            var lblMeta = new Label() { Text = "Meta:", Left = 10, Top = 110, Width = 60 };
            txtMeta = new TextBox() { Left = 80, Top = 106, Width = 690, ReadOnly = true };

            var lblSummary = new Label() { Text = "Summary:", Left = 10, Top = 140, Width = 60 };
            txtSummary = new TextBox() { Left = 80, Top = 136, Width = 690, Height = 360, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };

            btnExportJson = new Button() { Text = "Export JSON", Left = 420, Top = 10, Width = 100 };
            btnExportJson.Click += (s, e) => ExportJson();

            btnCopy = new Button() { Text = "Copy Summary", Left = 540, Top = 10, Width = 120 };
            btnCopy.Click += (s, e) => {
                if (!string.IsNullOrEmpty(txtSummary.Text))
                    Clipboard.SetText(txtSummary.Text);
            };

            this.Controls.Add(lblUrl);
            this.Controls.Add(txtUrl);
            this.Controls.Add(btnFetch);
            this.Controls.Add(lblStatus);
            this.Controls.Add(txtStatus);
            this.Controls.Add(lblTitle);
            this.Controls.Add(txtTitle);
            this.Controls.Add(lblMeta);
            this.Controls.Add(txtMeta);
            this.Controls.Add(lblSummary);
            this.Controls.Add(txtSummary);
            this.Controls.Add(btnExportJson);
            this.Controls.Add(btnCopy);
        }

        private async Task FetchAndAnalyzeAsync()
        {
            var url = txtUrl.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please enter a URL.", "Input required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            txtStatus.Text = "Fetching...";
            txtTitle.Text = string.Empty;
            txtMeta.Text = string.Empty;
            txtSummary.Text = string.Empty;

            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; WinFramework/1.0)");
                http.Timeout = TimeSpan.FromSeconds(15);

                var resp = await http.GetAsync(url);
                var statusCode = (int)resp.StatusCode;
                txtStatus.Text = statusCode.ToString();

                resp.EnsureSuccessStatusCode();

                var html = await resp.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var title = ExtractTitle(doc);
                var meta = ExtractMetaDescription(doc);
                var mainText = ExtractMainText(doc);
                var summary = SummarizeText(mainText);

                txtTitle.Text = title;
                txtMeta.Text = meta;
                txtSummary.Text = summary;
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Error";
                txtSummary.Text = ex.Message;
            }
        }

        private static string ExtractTitle(HtmlDocument doc)
        {
            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            if (titleNode != null)
                return CleanText(titleNode.InnerText);
            return "Untitled";
        }

        private static string ExtractMetaDescription(HtmlDocument doc)
        {
            var meta = doc.DocumentNode.SelectSingleNode("//meta[@name='description']")
                      ?? doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']");
            if (meta != null)
            {
                var content = meta.GetAttributeValue("content", string.Empty);
                if (!string.IsNullOrEmpty(content))
                    return CleanText(content);
            }
            return "";
        }

        private static string ExtractMainText(HtmlDocument doc)
        {
            // Try common containers
            var nodes = new[] { "//main", "//article", "//body" };
            foreach (var xpath in nodes)
            {
                var node = doc.DocumentNode.SelectSingleNode(xpath);
                if (node != null)
                {
                    var text = node.InnerText;
                    if (!string.IsNullOrWhiteSpace(text))
                        return CleanText(text);
                }
            }

            return CleanText(doc.DocumentNode.InnerText);
        }

        private static string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            text = text.Replace("\u00A0", " ");
            text = Regex.Replace(text, "\\s+", " ");
            return text.Trim();
        }

        private static string SummarizeText(string text, int maxSentences = 5)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var sentences = Regex.Split(text, "(?<=[.!?])\\s+");
            var list = new System.Collections.Generic.List<string>();
            foreach (var s in sentences)
            {
                var t = s.Trim();
                if (!string.IsNullOrEmpty(t)) list.Add(t);
                if (list.Count >= maxSentences) break;
            }
            return string.Join(" ", list);
        }

        private void ExportJson()
        {
            var obj = new
            {
                url = txtUrl.Text.Trim(),
                status = txtStatus.Text,
                title = txtTitle.Text,
                meta = txtMeta.Text,
                summary = txtSummary.Text
            };

            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });

            using var dlg = new SaveFileDialog();
            dlg.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            dlg.FileName = "winframework-result.json";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                System.IO.File.WriteAllText(dlg.FileName, json);
                MessageBox.Show("Exported to " + dlg.FileName, "Exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
