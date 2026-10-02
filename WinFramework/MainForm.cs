using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFramework.Controls;

namespace WinFramework
{
    public partial class MainForm : Form
    {
        // UI fields
        private Panel headerPanel;
        private Label lblHeaderTitle;
        private Label lblVersion;
        private Button btnMin;
        private Button btnMax;
        private Button btnClose;
        private Button btnMenu;

        private Panel leftPanel;
        private GroupBox grpNetwork;
        private Label lblProtocol;
        private ComboBox cbProtocol;
        private Label lblHost;
        private TextBox txtHost;
        private Label lblPort;
        private TextBox txtPort;
        private RoundedButton btnConnect;

        private GroupBox grpReceive;
        private CheckBox chkAsciiRecv;
        private CheckBox chkHexRecv;
        private CheckBox chkShowAsLog;
        private CheckBox chkAutoNewline;
        private CheckBox chkAutoSaveRecv;
        private LinkLabel lnkOrganizeRecv;
        private LinkLabel lnkClearRecv;

        private GroupBox grpSend;
        private CheckBox chkAsciiSend;
        private CheckBox chkHexSend;
        private CheckBox chkAutoParse;
        private CheckBox chkAtReturn;
        private CheckBox chkAutoChecksum;

        private Panel centerPanel;
        private GroupBox grpLog;
        private RichTextBox rtxtLog;
        private Label lblEmptyState;

        private Panel bottomPanel;
        private GroupBox grpDataSend;
        private TextBox txtSend;
        private RoundedButton btnSend;
        private RoundedButton btnClearSend;
        private RoundedButton btnClearLog;
        private RoundedButton btnUploadSample;

        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private ToolStripStatusLabel lblSpacer;
        private ToolStripStatusLabel lblCounts;
        private ToolStripStatusLabel lblCenterCounts;

        // Networking
        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private CancellationTokenSource? _readCts;
        private readonly object _syncRoot = new();

        private int _rxCount;
        private int _txCount;
        private readonly string _savePath = Path.Combine(Application.StartupPath, "recv_log.txt");

        public MainForm()
        {
            InitializeComponent();
            ApplyTheme();

            cbProtocol.SelectedIndex = 0;
            txtHost.Text = "192.168.6.101";
            txtPort.Text = "1234";
            txtSend.Text = "http://www.cmsoft.cn";
            lblStatus.Text = "就绪！";
            lblCounts.Text = "RX:0  TX:0";
            lblCenterCounts.Text = "0/0";

            UpdateHeaderPositions();
        }

        private void InitializeComponent()
        {
            // Form
            this.Text = "网络调试助手";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(1100, 720);
            this.MinimumSize = new Size(940, 640);
            this.DoubleBuffered = true;

            // Header
            headerPanel = new Panel { Dock = DockStyle.Top, Height = 72, Padding = new Padding(8) };

            btnMenu = new Button { Text = "≡", Size = new Size(38, 38), Location = new Point(10, 16), FlatStyle = FlatStyle.Flat };
            btnMenu.FlatAppearance.BorderSize = 0;
            btnMenu.ForeColor = Color.LightGray;
            btnMenu.BackColor = Color.FromArgb(12, 26, 40);

            var lblIcon = new Label { Text = "🌐", Font = new Font("Segoe UI Emoji", 20F), AutoSize = false, Width = 44, Height = 44, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(56, 14) };
            lblHeaderTitle = new Label { Text = "网络调试助手", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.White, AutoSize = false, Height = 44, TextAlign = ContentAlignment.MiddleLeft };
            lblHeaderTitle.Location = new Point(106, 14);
            lblHeaderTitle.Width = 620;

            lblVersion = new Label { Text = "NetAssist V4.3.2b", Font = new Font("Segoe UI", 9F), AutoSize = true };
            lblVersion.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            btnMin = new Button { Text = "—", Size = new Size(30, 26), FlatStyle = FlatStyle.Flat };
            btnMax = new Button { Text = "□", Size = new Size(30, 26), FlatStyle = FlatStyle.Flat };
            btnClose = new Button { Text = "✕", Size = new Size(30, 26), FlatStyle = FlatStyle.Flat };
            btnMin.Click += (_, _) => WindowState = FormWindowState.Minimized;
            btnMax.Click += (_, _) => WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
            btnClose.Click += (_, _) => Close();

            headerPanel.Controls.AddRange(new Control[] { btnMenu, lblIcon, lblHeaderTitle, lblVersion, btnMin, btnMax, btnClose });
            this.Controls.Add(headerPanel);

            // Left
            leftPanel = new Panel { Dock = DockStyle.Left, Width = 330, Padding = new Padding(12) };

            grpNetwork = new GroupBox { Text = "网络设置", Height = 188, Dock = DockStyle.Top };
            lblProtocol = new Label { Text = "(1) 协议类型", Location = new Point(12, 22) };
            cbProtocol = new ComboBox { Location = new Point(12, 48), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };

            lblHost = new Label { Text = "(2) 远程主机地址", Location = new Point(12, 88) };
            txtHost = new TextBox { Location = new Point(12, 110), Width = 260 };

            lblPort = new Label { Text = "(3) 远程主机端口", Location = new Point(12, 146) };
            txtPort = new TextBox { Location = new Point(12, 168), Width = 120 };

            btnConnect = new RoundedButton { Text = "连接", Location = new Point(188, 162), Width = 84, Height = 34, Radius = 6 };
            btnConnect.Click += BtnConnect_Click;

            grpNetwork.Controls.AddRange(new Control[] { lblProtocol, cbProtocol, lblHost, txtHost, lblPort, txtPort, btnConnect });

            grpReceive = new GroupBox { Text = "接收设置", Height = 150, Dock = DockStyle.Top };
            chkAsciiRecv = new CheckBox { Text = "ASCII", Location = new Point(12, 26), Checked = true };
            chkHexRecv = new CheckBox { Text = "HEX", Location = new Point(90, 26) };
            chkShowAsLog = new CheckBox { Text = "按日志模式显示", Location = new Point(12, 54), Checked = true };
            chkAutoNewline = new CheckBox { Text = "接收区自动换行", Location = new Point(12, 82), Checked = true };
            chkAutoSaveRecv = new CheckBox { Text = "接收区自动保存...", Location = new Point(12, 110) };

            lnkOrganizeRecv = new LinkLabel { Text = "整理接收", Location = new Point(188, 82), AutoSize = true };
            lnkClearRecv = new LinkLabel { Text = "清除接收", Location = new Point(188, 106), AutoSize = true };
            lnkClearRecv.LinkClicked += (s, e) => { rtxtLog.Clear(); ShowEmptyState(true); };

            grpReceive.Controls.AddRange(new Control[] { chkAsciiRecv, chkHexRecv, chkShowAsLog, chkAutoNewline, chkAutoSaveRecv, lnkOrganizeRecv, lnkClearRecv });

            grpSend = new GroupBox { Text = "发送设置", Height = 170, Dock = DockStyle.Top };
            chkAsciiSend = new CheckBox { Text = "ASCII", Location = new Point(12, 26), Checked = true };
            chkHexSend = new CheckBox { Text = "HEX", Location = new Point(92, 26) };
            chkAutoParse = new CheckBox { Text = "自动解析转义符", Location = new Point(12, 54), Checked = true };
            chkAtReturn = new CheckBox { Text = "AT指令自动回车", Location = new Point(12, 82) };
            chkAutoChecksum = new CheckBox { Text = "自动发送校验位", Location = new Point(12, 110) };

            grpSend.Controls.AddRange(new Control[] { chkAsciiSend, chkHexSend, chkAutoParse, chkAtReturn, chkAutoChecksum });

            leftPanel.Controls.AddRange(new Control[] { grpSend, grpReceive, grpNetwork });
            this.Controls.Add(leftPanel);

            // Center
            centerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            grpLog = new GroupBox { Text = "数据日志", Dock = DockStyle.Fill };
            rtxtLog = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 10F) };
            lblEmptyState = new Label { Text = "暂无数据", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 18F), ForeColor = Color.FromArgb(130, 160, 185) };

            grpLog.Controls.Add(rtxtLog);
            grpLog.Controls.Add(lblEmptyState);
            centerPanel.Controls.Add(grpLog);
            this.Controls.Add(centerPanel);

            // Bottom
            bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 128, Padding = new Padding(12) };
            grpDataSend = new GroupBox { Text = "数据发送", Dock = DockStyle.Fill };
            txtSend = new TextBox { Multiline = true, Left = 12, Top = 28, Width = 700, Height = 66, Text = "http://www.cmsoft.cn" };

            btnSend = new RoundedButton { Text = "发送", Left = 740, Top = 28, Width = 132, Height = 66, Radius = 10 };
            btnClearSend = new RoundedButton { Text = "清除", Left = 740, Top = 96, Width = 64, Height = 26, Radius = 6 };
            btnClearLog = new RoundedButton { Text = "清除日志", Left = 12, Top = 96, Width = 100, Height = 26, Radius = 6 };
            btnUploadSample = new RoundedButton { Text = "上传", Left = 812, Top = 96, Width = 60, Height = 26, Radius = 6 };

            btnSend.Click += BtnSend_Click;
            btnClearSend.Click += (s, e) => txtSend.Clear();
            btnClearLog.Click += (s, e) => { rtxtLog.Clear(); ShowEmptyState(true); };

            grpDataSend.Controls.AddRange(new Control[] { txtSend, btnSend, btnClearSend, btnClearLog, btnUploadSample });
            bottomPanel.Controls.Add(grpDataSend);
            this.Controls.Add(bottomPanel);

            // StatusStrip
            statusStrip = new StatusStrip { BackColor = Color.FromArgb(8, 18, 28), ForeColor = Color.LightGray };
            lblStatus = new ToolStripStatusLabel("就绪！");
            lblSpacer = new ToolStripStatusLabel { Spring = true };
            lblCounts = new ToolStripStatusLabel("RX:0  TX:0");
            lblCenterCounts = new ToolStripStatusLabel("0/0") { Margin = new Padding(8, 0, 8, 0) };
            statusStrip.Items.Add(lblStatus);
            statusStrip.Items.Add(lblSpacer);
            statusStrip.Items.Add(lblCenterCounts);
            statusStrip.Items.Add(lblCounts);
            this.Controls.Add(statusStrip);

            // Wire fields to previously used names
            this.lblHeaderTitle = lblHeaderTitle; // already assigned
            this.lblVersion = lblVersion;
            this.btnMin = btnMin; this.btnMax = btnMax; this.btnClose = btnClose; this.btnMenu = btnMenu;
            this.leftPanel = leftPanel; this.grpNetwork = grpNetwork; this.cbProtocol = cbProtocol; this.txtHost = txtHost; this.txtPort = txtPort; this.btnConnect = btnConnect;
            this.grpReceive = grpReceive; this.chkAsciiRecv = chkAsciiRecv; this.chkHexRecv = chkHexRecv; this.chkShowAsLog = chkShowAsLog; this.chkAutoNewline = chkAutoNewline; this.chkAutoSaveRecv = chkAutoSaveRecv; this.lnkOrganizeRecv = lnkOrganizeRecv; this.lnkClearRecv = lnkClearRecv;
            this.grpSend = grpSend; this.chkAsciiSend = chkAsciiSend; this.chkHexSend = chkHexSend; this.chkAutoParse = chkAutoParse; this.chkAtReturn = chkAtReturn; this.chkAutoChecksum = chkAutoChecksum;
            this.centerPanel = centerPanel; this.grpLog = grpLog; this.rtxtLog = rtxtLog; this.lblEmptyState = lblEmptyState;
            this.bottomPanel = bottomPanel; this.grpDataSend = grpDataSend; this.txtSend = txtSend; this.btnSend = btnSend; this.btnClearSend = btnClearSend; this.btnClearLog = btnClearLog; this.btnUploadSample = btnUploadSample;
            this.statusStrip = statusStrip; this.lblStatus = lblStatus; this.lblCounts = lblCounts; this.lblCenterCounts = lblCenterCounts; this.lblSpacer = lblSpacer;

            // header drag
            headerPanel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) NativeMethods.ReleaseCaptureAndDrag(this.Handle); };
            this.Resize += (s, e) => UpdateHeaderPositions();
        }

        private void ApplyTheme()
        {
            // Colors and fonts tuned to match the reference screenshot
            this.BackColor = Color.FromArgb(5, 18, 28);
            foreach (Control c in this.Controls)
                c.Font = new Font("Segoe UI", 9F);

            lblHeaderTitle.ForeColor = Color.FromArgb(208, 235, 255);
            lblVersion.ForeColor = Color.FromArgb(120, 205, 255);

            leftPanel.BackColor = Color.FromArgb(9, 24, 38);
            centerPanel.BackColor = Color.FromArgb(8, 18, 28);
            bottomPanel.BackColor = Color.FromArgb(6, 16, 26);

            StyleGroup(grpNetwork);
            StyleGroup(grpReceive);
            StyleGroup(grpSend);
            StyleGroup(grpLog);
            StyleGroup(grpDataSend);

            rtxtLog.BackColor = Color.FromArgb(7, 16, 24);
            rtxtLog.ForeColor = Color.FromArgb(180, 210, 230);
            rtxtLog.Font = new Font("Consolas", 11F);

            lblEmptyState.ForeColor = Color.FromArgb(120, 155, 185);

            // Header buttons placement and style
            btnMin.BackColor = Color.FromArgb(12, 28, 40);
            btnMax.BackColor = Color.FromArgb(12, 28, 40);
            btnClose.BackColor = Color.FromArgb(12, 28, 40);
            btnMin.ForeColor = Color.LightGray; btnMax.ForeColor = Color.LightGray; btnClose.ForeColor = Color.LightGray;

            btnMenu.BackColor = Color.FromArgb(12, 28, 40);
            btnMenu.ForeColor = Color.LightGray;

            // Rounded button default colors handled in control; tweak specific ones
            btnConnect.BackColor = Color.FromArgb(34, 123, 233);
            btnSend.BackColor = Color.FromArgb(38, 184, 104);
            btnClearSend.BackColor = Color.FromArgb(22, 36, 48);
            btnClearLog.BackColor = Color.FromArgb(22, 36, 48);
            btnUploadSample.BackColor = Color.FromArgb(60, 100, 180);

            UpdateHeaderPositions();
            ShowEmptyState(true);
        }

        private void UpdateHeaderPositions()
        {
            // locate right side header elements
            if (lblVersion != null)
                lblVersion.Location = new Point(this.ClientSize.Width - 160, 18);
            if (btnClose != null)
                btnClose.Location = new Point(this.ClientSize.Width - 40, 18);
            if (btnMax != null)
                btnMax.Location = new Point(this.ClientSize.Width - 80, 18);
            if (btnMin != null)
                btnMin.Location = new Point(this.ClientSize.Width - 120, 18);
        }

        // Network logic reused from previous implementation
        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (_tcpClient != null && _tcpClient.Connected)
            {
                await DisconnectAsync();
                return;
            }

            string host = txtHost.Text.Trim();
            if (!int.TryParse(txtPort.Text.Trim(), out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("端口号无效。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var tcp = new TcpClient();
                await tcp.ConnectAsync(host, port);

                lock (_syncRoot)
                {
                    _tcpClient = tcp;
                    _networkStream = tcp.GetStream();
                    _readCts = new CancellationTokenSource();
                }

                btnConnect.Text = "断开";
                lblStatus.Text = "已连接";
                AppendLogLine($"[SYS] {DateTime.Now:HH:mm:ss} 已连接 {host}:{port}");
                _ = Task.Run(() => ReadLoopAsync(_readCts.Token));
            }
            catch (Exception ex)
            {
                AppendLogLine($"[ERR] {DateTime.Now:HH:mm:ss} 连接失败: {ex.Message}");
                lblStatus.Text = "连接失败";
            }
        }

        private async Task DisconnectAsync()
        {
            _readCts?.Cancel();
            if (_networkStream != null) { try { await _networkStream.FlushAsync(); } catch { } _networkStream.Dispose(); }
            if (_tcpClient != null) { _tcpClient.Close(); _tcpClient.Dispose(); }
            lock (_syncRoot) { _networkStream = null; _tcpClient = null; }
            btnConnect.Text = "连接";
            lblStatus.Text = "就绪！";
            AppendLogLine($"[SYS] {DateTime.Now:HH:mm:ss} 已断开连接");
        }

        private async Task ReadLoopAsync(CancellationToken token)
        {
            var buffer = new byte[4096];
            while (!token.IsCancellationRequested)
            {
                int read;
                try
                {
                    if (_networkStream == null) return;
                    read = await _networkStream.ReadAsync(buffer, 0, buffer.Length, token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    BeginInvoke((Action)(() => { AppendLogLine($"[ERR] {DateTime.Now:HH:mm:ss} 读取异常: {ex.Message}"); lblStatus.Text = "连接异常"; }));
                    break;
                }

                if (read <= 0) { BeginInvoke((Action)(() => { AppendLogLine($"[SYS] {DateTime.Now:HH:mm:ss} 远端关闭连接"); btnConnect.Text = "连接"; lblStatus.Text = "就绪！"; })); break; }

                var chunk = new byte[read]; Array.Copy(buffer, chunk, read);
                BeginInvoke((Action)(() => HandleReceivedBytes(chunk)));
            }
        }

        private void HandleReceivedBytes(byte[] data)
        {
            var text = chkAsciiRecv.Checked ? Encoding.ASCII.GetString(data) : ByteArrayToHex(data);
            AppendLogLine($"[RX] {DateTime.Now:HH:mm:ss} {text}");
            if (chkAutoSaveRecv.Checked) { try { File.AppendAllText(_savePath, text + Environment.NewLine); } catch { } }
            _rxCount += data.Length; _rxCount = Math.Max(0, _rxCount);
            lblCounts.Text = $"RX:{_rxCount}  TX:{_txCount}";
            lblCenterCounts.Text = $"{_rxCount}/{_txCount}";
            ShowEmptyState(false);
        }

        private void BtnSend_Click(object? sender, EventArgs e)
        {
            if (_networkStream == null || _tcpClient == null || !_tcpClient.Connected)
            {
                MessageBox.Show("请先连接设备。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var input = txtSend.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;
            if (chkAutoParse.Checked) input = ExpandEscapeSequences(input);
            if (chkAtReturn.Checked && !input.EndsWith("\r") && !input.EndsWith("\n")) input += "\r";
            var data = PrepareSendBytes(input);
            try
            {
                _networkStream.Write(data, 0, data.Length);
                _networkStream.Flush();
                var display = chkAsciiSend.Checked ? Encoding.ASCII.GetString(data) : ByteArrayToHex(data);
                AppendLogLine($"[TX] {DateTime.Now:HH:mm:ss} {display}");
                _txCount += data.Length; lblCounts.Text = $"RX:{_rxCount}  TX:{_txCount}"; lblCenterCounts.Text = $"{_rxCount}/{_txCount}"; ShowEmptyState(false);
            }
            catch (Exception ex) { AppendLogLine($"[ERR] {DateTime.Now:HH:mm:ss} 发送失败: {ex.Message}"); }
        }

        private void AppendLogLine(string line)
        {
            if (rtxtLog.InvokeRequired) { rtxtLog.BeginInvoke((Action)(() => AppendLogLine(line))); return; }
            if (string.IsNullOrEmpty(rtxtLog.Text)) ShowEmptyState(false);
            rtxtLog.AppendText(line + Environment.NewLine);
            rtxtLog.ScrollToCaret();
        }

        private void ShowEmptyState(bool show)
        {
            lblEmptyState.Visible = show && string.IsNullOrEmpty(rtxtLog.Text);
            rtxtLog.Visible = !show || !string.IsNullOrEmpty(rtxtLog.Text);
        }

        private static string ExpandEscapeSequences(string input) => input.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\0", "\0");

        private byte[] PrepareSendBytes(string input)
        {
            if (chkHexSend.Checked)
            {
                var clean = input.Replace(" ", string.Empty).Replace("\t", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
                if (clean.Length % 2 != 0) clean = clean.Substring(0, clean.Length - 1);
                try { return HexToByteArray(clean); } catch { return Encoding.ASCII.GetBytes(input); }
            }
            return Encoding.ASCII.GetBytes(input);
        }

        private static string ByteArrayToHex(byte[] data) => BitConverter.ToString(data).Replace("-", " ");
        private static byte[] HexToByteArray(string hex) { if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>(); if (hex.Length % 2 != 0) throw new ArgumentException("Hex length invalid."); var res = new byte[hex.Length / 2]; for (int i = 0; i < res.Length; i++) res[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16); return res; }

        protected override void OnClosed(EventArgs e) { _readCts?.Cancel(); _networkStream?.Dispose(); _tcpClient?.Dispose(); base.OnClosed(e); }
    }
}
