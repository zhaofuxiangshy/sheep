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
        // UI fields omitted for brevity are declared in InitializeComponent

        public MainForm()
        {
            InitializeComponent();
            ApplyTheme();
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
            var header = new Panel { Dock = DockStyle.Top, Height = 72, Padding = new Padding(12) };
            var lblIcon = new Label { Text = "🌐", Font = new Font("Segoe UI Emoji", 20F), AutoSize = false, Width = 44, Height = 44, TextAlign = ContentAlignment.MiddleCenter };
            var lblTitle = new Label { Text = "网络调试助手", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.White, AutoSize = false, Height = 44, TextAlign = ContentAlignment.MiddleLeft };
            lblTitle.Location = new Point(64, 16);
            lblTitle.Width = 700;

            var lblVer = new Label { Text = "NetAssist V4.3.2b", Font = new Font("Segoe UI", 9F), AutoSize = true };
            lblVer.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var btnMin = new Button { Text = "—", Size = new Size(30, 28), FlatStyle = FlatStyle.Flat };
            var btnMax = new Button { Text = "□", Size = new Size(30, 28), FlatStyle = FlatStyle.Flat };
            var btnClose = new Button { Text = "✕", Size = new Size(30, 28), FlatStyle = FlatStyle.Flat };
            btnMin.Click += (_, _) => WindowState = FormWindowState.Minimized;
            btnMax.Click += (_, _) => WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
            btnClose.Click += (_, _) => Close();

            header.Controls.Add(lblIcon);
            header.Controls.Add(lblTitle);
            header.Controls.Add(lblVer);
            header.Controls.Add(btnMin);
            header.Controls.Add(btnMax);
            header.Controls.Add(btnClose);
            this.Controls.Add(header);

            // Left
            var left = new Panel { Dock = DockStyle.Left, Width = 320, Padding = new Padding(12) };

            var grpNetwork = new GroupBox { Text = "网络设置", Height = 188, Dock = DockStyle.Top };
            var lblProt = new Label { Text = "(1) 协议类型", Location = new Point(12, 22) };
            var cbProt = new ComboBox { Location = new Point(12, 46), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            cbProt.Items.AddRange(new object[] { "TCP Client", "TCP Server", "UDP" });
            cbProt.SelectedIndex = 0;
            var lblHost = new Label { Text = "(2) 远程主机地址", Location = new Point(12, 86) };
            var txtHost = new TextBox { Location = new Point(12, 108), Width = 260, Text = "192.168.6.101" };
            var lblPort = new Label { Text = "(3) 远程主机端口", Location = new Point(12, 142) };
            var txtPort = new TextBox { Location = new Point(12, 164), Width = 120, Text = "1234" };

            var btnConnect = new RoundedButton { Text = "连接", Location = new Point(184, 160), Width = 88, Height = 34, Radius = 6 };
            btnConnect.Click += BtnConnect_Click;

            grpNetwork.Controls.AddRange(new Control[] { lblProt, cbProt, lblHost, txtHost, lblPort, txtPort, btnConnect });

            var grpRecv = new GroupBox { Text = "接收设置", Height = 150, Dock = DockStyle.Top };
            var chkAsciiR = new CheckBox { Text = "ASCII", Location = new Point(12, 28), Checked = true };
            var chkHexR = new CheckBox { Text = "HEX", Location = new Point(92, 28) };
            var chkLogMode = new CheckBox { Text = "按日志模式显示", Location = new Point(12, 58), Checked = true };
            var chkWrap = new CheckBox { Text = "接收区自动换行", Location = new Point(12, 86), Checked = true };
            var chkAutoSave = new CheckBox { Text = "接收区自动保存...", Location = new Point(12, 112) };
            grpRecv.Controls.AddRange(new Control[] { chkAsciiR, chkHexR, chkLogMode, chkWrap, chkAutoSave });

            var grpSend = new GroupBox { Text = "发送设置", Height = 170, Dock = DockStyle.Top };
            var chkAsciiS = new CheckBox { Text = "ASCII", Location = new Point(12, 28), Checked = true };
            var chkHexS = new CheckBox { Text = "HEX", Location = new Point(92, 28) };
            var chkParse = new CheckBox { Text = "自动解析转义符", Location = new Point(12, 58), Checked = true };
            var chkAt = new CheckBox { Text = "AT指令自动回车", Location = new Point(12, 86) };
            var chkCheck = new CheckBox { Text = "自动发送校验位", Location = new Point(12, 114) };
            grpSend.Controls.AddRange(new Control[] { chkAsciiS, chkHexS, chkParse, chkAt, chkCheck });

            left.Controls.AddRange(new Control[] { grpSend, grpRecv, grpNetwork });
            this.Controls.Add(left);

            // Center
            var center = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            var grpLog = new GroupBox { Text = "数据日志", Dock = DockStyle.Fill };
            var rtxt = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 10F) };
            var lblEmpty = new Label { Text = "暂无数据", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 18F), ForeColor = Color.FromArgb(130, 160, 185) };
            grpLog.Controls.Add(rtxt);
            grpLog.Controls.Add(lblEmpty);
            center.Controls.Add(grpLog);
            this.Controls.Add(center);

            // Bottom
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 128, Padding = new Padding(12) };
            var grpSendArea = new GroupBox { Text = "数据发送", Dock = DockStyle.Fill };
            var txtSend = new TextBox { Multiline = true, Left = 12, Top = 24, Width = 720, Height = 64, Text = "http://www.cmsoft.cn" };
            var btnSend = new RoundedButton { Text = "发送", Left = 760, Top = 24, Width = 128, Height = 64, Radius = 8 };
            var btnClear = new RoundedButton { Text = "清除", Left = 760, Top = 92, Width = 64, Height = 28, Radius = 6 };
            var btnClearLog = new RoundedButton { Text = "清除日志", Left = 12, Top = 92, Width = 100, Height = 28, Radius = 6 };

            btnSend.Click += (s, e) =>
            {
                // delegate to existing send logic
                BtnSend_Click(s, e);
            };
            btnClear.Click += (s, e) => txtSend.Clear();
            btnClearLog.Click += (s, e) => rtxt.Clear();

            grpSendArea.Controls.AddRange(new Control[] { txtSend, btnSend, btnClear, btnClearLog });
            bottom.Controls.Add(grpSendArea);
            this.Controls.Add(bottom);

            // StatusStrip
            var status = new StatusStrip();
            var lblState = new ToolStripStatusLabel { Text = "就绪！" };
            var lblCtr = new ToolStripStatusLabel { Text = "RX:0  TX:0", Spring = false };
            status.Items.Add(lblState);
            status.Items.Add(new ToolStripStatusLabel { Spring = true });
            status.Items.Add(lblCtr);
            this.Controls.Add(status);

            // Wire common controls to fields for use elsewhere
            // Using naming similar to earlier files for compatibility
            this.lblHeaderTitle = lblTitle;
            this.lblVersion = lblVer;
            this.btnMin = btnMin;
            this.btnMax = btnMax;
            this.btnClose = btnClose;

            // left controls
            this.leftPanel = left;
            this.grpNetwork = grpNetwork;
            this.cbProtocol = cbProt;
            this.txtHost = txtHost;
            this.txtPort = txtPort;
            this.btnConnect = btnConnect;

            this.grpReceive = grpRecv;
            this.chkAsciiRecv = chkAsciiR;
            this.chkHexRecv = chkHexR;
            this.chkShowAsLog = chkLogMode;
            this.chkAutoNewline = chkWrap;
            this.chkAutoSaveRecv = chkAutoSave;

            this.grpSend = grpSend;
            this.chkAsciiSend = chkAsciiS;
            this.chkHexSend = chkHexS;
            this.chkAutoParse = chkParse;
            this.chkAtReturn = chkAt;
            this.chkAutoChecksum = chkCheck;

            this.centerPanel = center;
            this.grpLog = grpLog;
            this.rtxtLog = rtxt;
            this.lblEmptyState = lblEmpty;

            this.bottomPanel = bottom;
            this.grpDataSend = grpSendArea;
            this.txtSend = txtSend;
            this.btnSend = btnSend;
            this.btnClearSend = btnClear;
            this.btnClearLog = btnClearLog;

            this.statusStrip = status;
            this.lblStatus = lblState;
            this.lblCounts = lblCtr;

            // header drag
            header.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) NativeMethods.ReleaseCaptureAndDrag(this.Handle); };
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

            leftPanel.BackColor = Color.FromArgb(9, 24, 36);
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

            // Rounded button default colors handled in control; tweak specific ones
            btnConnect.BackColor = Color.FromArgb(34, 123, 233);
            btnSend.BackColor = Color.FromArgb(38, 184, 104);
            btnClearSend.BackColor = Color.FromArgb(22, 36, 48);
            btnClearLog.BackColor = Color.FromArgb(22, 36, 48);

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

        // Existing network and send/receive code expects fields with same names
        // We'll reuse the earlier implementations for Connect, Send, ReadLoop, etc.

        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private CancellationTokenSource? _readCts;
        private readonly object _syncRoot = new object();
        private int _rxCount = 0;
        private int _txCount = 0;

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
                _txCount += data.Length; lblCounts.Text = $"RX:{_rxCount}  TX:{_txCount}"; ShowEmptyState(false);
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
