using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace DzfpPdfPrinter
{
    public partial class MainForm : Form
    {
        private FileMonitorService? _monitorService;
        private TextBox? txtDirectory;
        private Button? btnBrowse;
        private ComboBox? cmbPrinters;
        private Button? btnStartStop;
        private Button? btnClearHistory;
        private ListBox? lstLog;
        private Label? lblStatus;
        private Label? lblProcessedCount;
        private Label? lblFailedCount;
        private Button? btnRetryFailed;
        private Button? btnCheckPdfTools;
        private CheckBox? chkAutoStart;
        private ComboBox? cmbPrintScale;
        private Label? lblPrintScale;
        private NotifyIcon? notifyIcon;
        private ContextMenuStrip? trayMenu;
        private bool _startMinimized;

        public MainForm(bool startMinimized = false)
        {
            _startMinimized = startMinimized;
            InitializeComponent();
            InitializeTrayIcon();
            LoadPrinters();
            LoadSettings();
            CheckPdfPrintTools();
            LoadPrintScaleSetting();
            
            if (_startMinimized)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Visible = false;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "DZFP PDF 自动打印工具";
            this.Size = new Size(700, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimumSize = new Size(700, 620);

            var grpSettings = new GroupBox
            {
                Text = "监控设置",
                Location = new Point(12, 12),
                Size = new Size(654, 175),
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            var lblDirectory = new Label
            {
                Text = "监控目录:",
                Location = new Point(15, 28),
                Size = new Size(70, 23),
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            txtDirectory = new TextBox
            {
                Location = new Point(91, 25),
                Size = new Size(450, 26),
                Font = new Font("Microsoft YaHei UI", 9F),
                ReadOnly = true
            };

            btnBrowse = new Button
            {
                Text = "选择目录...",
                Location = new Point(547, 24),
                Size = new Size(95, 28),
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            btnBrowse.Click += BtnBrowse_Click!;

            var lblPrinter = new Label
            {
                Text = "打印机:",
                Location = new Point(15, 68),
                Size = new Size(70, 23),
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            cmbPrinters = new ComboBox
            {
                Location = new Point(91, 65),
                Size = new Size(450, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            cmbPrinters.Items.Add("(默认打印机)");

            btnStartStop = new Button
            {
                Text = "▶ 开始监控",
                Location = new Point(547, 64),
                Size = new Size(95, 28),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
            };
            btnStartStop.Click += BtnStartStop_Click!;
            btnStartStop.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 180);

            lblPrintScale = new Label
            {
                Text = "打印模式:",
                Location = new Point(200, 100),
                Size = new Size(70, 23),
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            cmbPrintScale = new ComboBox
            {
                Location = new Point(275, 97),
                Size = new Size(180, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            cmbPrintScale.Items.AddRange(new[] { "适应页面 (推荐)", "缩小适应", "实际大小" });
            cmbPrintScale.SelectedIndex = 0;
            cmbPrintScale.SelectedIndexChanged += CmbPrintScale_SelectedIndexChanged!;

            chkAutoStart = new CheckBox
            {
                Text = "开机自动启动",
                Location = new Point(15, 130),
                Size = new Size(150, 24),
                Font = new Font("Microsoft YaHei UI", 9F),
                Checked = AutoStartManager.IsEnabled
            };
            chkAutoStart.CheckedChanged += ChkAutoStart_CheckedChanged!;

            grpSettings.Controls.AddRange(new Control[] { lblDirectory, txtDirectory, btnBrowse, lblPrinter, cmbPrinters, btnStartStop, lblPrintScale, cmbPrintScale, chkAutoStart });
            this.Controls.Add(grpSettings);

            var grpLog = new GroupBox
            {
                Text = "运行日志",
                Location = new Point(12, 140),
                Size = new Size(654, 320),
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            lstLog = new ListBox
            {
                Location = new Point(15, 25),
                Size = new Size(624, 280),
                Font = new Font("Consolas", 9F),
                IntegralHeight = false,
                ScrollAlwaysVisible = true,
                SelectionMode = SelectionMode.MultiExtended
            };
            lstLog.KeyDown += LstLog_KeyDown!;
            lstLog.MouseDown += LstLog_MouseDown!;

            grpLog.Controls.Add(lstLog);
            this.Controls.Add(grpLog);

            var pnlBottom = new Panel
            {
                Location = new Point(12, 468),
                Size = new Size(654, 60),
                BorderStyle = BorderStyle.None
            };

            lblStatus = new Label
            {
                Text = "● 就绪",
                Location = new Point(15, 5),
                Size = new Size(150, 20),
                ForeColor = Color.Gray,
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            lblProcessedCount = new Label
            {
                Text = "已处理: 0 个",
                Location = new Point(170, 5),
                Size = new Size(110, 20),
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            lblFailedCount = new Label
            {
                Text = "失败: 0 个",
                Location = new Point(285, 5),
                Size = new Size(90, 20),
                ForeColor = Color.Red,
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            btnRetryFailed = new Button
            {
                Text = "重试失败文件",
                Location = new Point(15, 30),
                Size = new Size(120, 26),
                Font = new Font("Microsoft YaHei UI", 9F),
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnRetryFailed.Click += BtnRetryFailed_Click!;

            btnClearHistory = new Button
            {
                Text = "清除历史记录",
                Location = new Point(145, 30),
                Size = new Size(120, 26),
                Font = new Font("Microsoft YaHei UI", 9F),
                FlatStyle = FlatStyle.Flat
            };
            btnClearHistory.Click += BtnClearHistory_Click!;

            btnCheckPdfTools = new Button
            {
                Text = "PDF工具状态",
                Location = new Point(275, 30),
                Size = new Size(100, 26),
                Font = new Font("Microsoft YaHei UI", 9F),
                FlatStyle = FlatStyle.Flat
            };
            btnCheckPdfTools.Click += BtnCheckPdfTools_Click!;

            var btnBrowseSumatra = new Button
            {
                Text = "浏览...",
                Location = new Point(383, 30),
                Size = new Size(70, 26),
                Font = new Font("Microsoft YaHei UI", 9F),
                FlatStyle = FlatStyle.Flat
            };
            btnBrowseSumatra.Click += BtnBrowseSumatra_Click!;

            pnlBottom.Controls.AddRange(new Control[] { lblStatus, lblProcessedCount, lblFailedCount, btnRetryFailed, btnClearHistory, btnCheckPdfTools, btnBrowseSumatra });
            this.Controls.Add(pnlBottom);

            this.FormClosing += MainForm_FormClosing!;
        }

        private void InitializeTrayIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "DZFP PDF 自动打印工具",
                Visible = true
            };

            trayMenu = new ContextMenuStrip();
            
            var showItem = new ToolStripMenuItem("显示主窗口", null, (s, e) => ShowMainWindow());
            var startStopItem = new ToolStripMenuItem("开始监控", null, (s, e) => 
            {
                if (_monitorService?.IsMonitoring == true)
                    StopMonitoring();
                else
                    _ = StartMonitoringAsync();
            });
            
            var separator1 = new ToolStripSeparator();
            var exitItem = new ToolStripMenuItem("退出", null, (s, e) => ExitApplication());

            trayMenu.Items.AddRange(new ToolStripItem[] { showItem, startStopItem, separator1, exitItem });
            
            notifyIcon.ContextMenuStrip = trayMenu;
            notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
            
            notifyIcon.BalloonTipTitle = "DZFP PDF 自动打印工具";
            notifyIcon.BalloonTipText = "程序已在系统托盘中运行，双击图标显示主窗口";
            if (_startMinimized)
            {
                notifyIcon.ShowBalloonTip(3000);
            }
        }

        private void ShowMainWindow()
        {
            this.ShowInTaskbar = true;
            this.Visible = true;
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            this.BringToFront();
        }

        private void HideToTray()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
        }

        private void ExitApplication()
        {
            if (_monitorService?.IsMonitoring == true)
            {
                _monitorService.StopMonitoring();
            }
            
            notifyIcon?.Dispose();
            trayMenu?.Dispose();
            Application.Exit();
        }

        private void LoadPrinters()
        {
            try
            {
                foreach (var printer in PrinterSettings.InstalledPrinters.Cast<string>())
                {
                    cmbPrinters?.Items.Add(printer);
                }
                
                if (cmbPrinters?.Items.Count > 1)
                {
                    cmbPrinters.SelectedIndex = 0;
                    
                    var defaultPrinter = new PrinterSettings().PrinterName;
                    for (int i = 0; i < cmbPrinters.Items.Count; i++)
                    {
                        if (cmbPrinters.Items[i]?.ToString() == defaultPrinter)
                        {
                            cmbPrinters.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"加载打印机列表失败: {ex.Message}", Color.Red);
            }
        }

        private void LoadSettings()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "DzfpPdfPrinter", 
                    "settings.txt"
                );
                
                if (File.Exists(settingsPath))
                {
                    var lines = File.ReadAllLines(settingsPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("Directory=") && txtDirectory != null)
                        {
                            txtDirectory.Text = line.Substring("Directory=".Length);
                        }
                        else if (line.StartsWith("Printer=") && cmbPrinters != null)
                        {
                            var printerName = line.Substring("Printer=".Length);
                            for (int i = 0; i < cmbPrinters.Items.Count; i++)
                            {
                                if (cmbPrinters.Items[i]?.ToString() == printerName)
                                {
                                    cmbPrinters.SelectedIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadPrintScaleSetting()
        {
            try
            {
                var mode = PdfPrinter.GetPrintScaleMode();
                
                if (cmbPrintScale != null)
                {
                    switch (mode)
                    {
                        case PdfPrinter.PrintScaleMode.FitToPage:
                            cmbPrintScale.SelectedIndex = 0;
                            break;
                        case PdfPrinter.PrintScaleMode.ShrinkToFit:
                            cmbPrintScale.SelectedIndex = 1;
                            break;
                        case PdfPrinter.PrintScaleMode.None:
                        default:
                            cmbPrintScale.SelectedIndex = 2;
                            break;
                    }
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                var appData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "DzfpPdfPrinter"
                );
                
                if (!Directory.Exists(appData))
                {
                    Directory.CreateDirectory(appData);
                }

                var settingsPath = Path.Combine(appData, "settings.txt");
                var lines = new List<string>
                {
                    $"Directory={txtDirectory?.Text ?? ""}",
                    $"Printer={cmbPrinters?.SelectedItem?.ToString() ?? ""}"
                };
                File.WriteAllLines(settingsPath, lines);
            }
            catch { }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择要监控的目录（将自动打印新增的 dzfp_*.pdf 文件）",
                ShowNewFolderButton = false,
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == DialogResult.OK && txtDirectory != null)
            {
                txtDirectory.Text = dialog.SelectedPath;
            }
        }

        private async void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (_monitorService?.IsMonitoring == true)
            {
                StopMonitoring();
            }
            else
            {
                await StartMonitoringAsync();
            }
        }

        private async Task StartMonitoringAsync()
        {
            if (string.IsNullOrWhiteSpace(txtDirectory?.Text))
            {
                MessageBox.Show(
                    "请先选择要监控的目录！", 
                    "提示", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (!Directory.Exists(txtDirectory.Text))
            {
                MessageBox.Show(
                    "选择的目录不存在！", 
                    "错误", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error
                );
                return;
            }

            try
            {
                btnStartStop!.Enabled = false;
                btnStartStop.Text = "启动中...";
                
                string? printerName = null;
                if (cmbPrinters?.SelectedIndex > 0)
                {
                    printerName = cmbPrinters.SelectedItem?.ToString();
                }

                _monitorService = new FileMonitorService();
                _monitorService.OnFileDetected += (fileName, filePath) =>
                {
                    this.Invoke(() =>
                    {
                        AddLog($"[检测] 发现新文件: {fileName}", Color.Blue);
                    });
                };

                _monitorService.OnFilePrinted += (fileName) =>
                {
                    this.Invoke(() =>
                    {
                        AddLog($"[打印] 成功: {fileName}", Color.Green);
                        UpdateProcessedCount();
                    });
                };

                _monitorService.OnError += (title, message) =>
                {
                    this.Invoke(() =>
                    {
                        AddLog($"[错误] {title}: {message}", Color.Red);
                    });
                };

                _monitorService.OnFileFailed += (fileName) =>
                {
                    this.Invoke(() =>
                    {
                        AddLog($"[失败] 最终失败: {fileName} (已重试3次)", Color.DarkRed);
                        UpdateStatusCounts();
                        btnRetryFailed!.Enabled = true;
                    });
                };

                await Task.Run(() => 
                {
                    _monitorService.StartMonitoring(txtDirectory.Text, printerName);
                });

                UpdateUIForMonitoring(true);
                AddLog($"✓ 监控已启动: {txtDirectory.Text}", Color.DarkGreen);
                AddLog("   等待新的 dzfp_*.pdf 文件...", Color.Gray);
                SaveSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateUIForMonitoring(false);
            }
            finally
            {
                btnStartStop.Enabled = true;
            }
        }

        private void StopMonitoring()
        {
            try
            {
                _monitorService?.StopMonitoring();
                UpdateUIForMonitoring(false);
                AddLog("✓ 监控已停止", Color.Orange);
            }
            catch (Exception ex)
            {
                AddLog($"停止监控时出错: {ex.Message}", Color.Red);
            }
        }

        private void UpdateUIForMonitoring(bool isMonitoring)
        {
            if (isMonitoring)
            {
                btnStartStop!.Text = "■ 停止监控";
                btnStartStop.BackColor = Color.FromArgb(220, 53, 69);
                btnStartStop.FlatAppearance.BorderColor = Color.FromArgb(198, 40, 56);
                lblStatus!.Text = "● 监控中...";
                lblStatus.ForeColor = Color.Green;
                txtDirectory!.ReadOnly = true;
                btnBrowse!.Enabled = false;
                cmbPrinters!.Enabled = false;
            }
            else
            {
                btnStartStop!.Text = "▶ 开始监控";
                btnStartStop.BackColor = Color.FromArgb(0, 122, 204);
                btnStartStop.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 180);
                lblStatus!.Text = "● 已停止";
                lblStatus.ForeColor = Color.Gray;
                txtDirectory!.ReadOnly = false;
                btnBrowse!.Enabled = true;
                cmbPrinters!.Enabled = true;
            }
        }

        private void LstLog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelectedLogs();
                e.Handled = true;
            }
        }

        private void LstLog_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Clicks == 2)
            {
                var index = lstLog!.IndexFromPoint(e.Location);
                if (index >= 0)
                {
                    CopySingleLog(index);
                }
            }
        }

        private void CopySelectedLogs()
        {
            if (lstLog == null) return;

            if (lstLog.SelectedItems.Count > 0)
            {
                var selectedText = new List<string>();
                foreach (var item in lstLog.SelectedItems.Cast<object>())
                {
                    selectedText.Add(item.ToString() ?? "");
                }
                
                var textToCopy = string.Join(Environment.NewLine, selectedText);
                Clipboard.SetText(textToCopy);
                
                AddLog($"✓ 已复制 {lstLog.SelectedItems.Count} 条日志", Color.Gray);
            }
            else
            {
                CopyAllLogs();
            }
        }

        private void CopySingleLog(int index)
        {
            if (lstLog == null || index < 0 || index >= lstLog.Items.Count) return;

            var logText = lstLog.Items[index]?.ToString() ?? "";
            Clipboard.SetText(logText);
            
            AddLog($"✓ 已复制: {logText.Substring(0, Math.Min(50, logText.Length))}...", Color.Gray);
        }

        private void CopyAllLogs()
        {
            if (lstLog == null) return;

            var allText = new List<string>();
            foreach (var item in lstLog.Items.Cast<object>())
            {
                allText.Add(item.ToString() ?? "");
            }

            var textToCopy = string.Join(Environment.NewLine, allText);
            Clipboard.SetText(textToCopy);
            
            AddLog($"✓ 已复制全部 {lstLog.Items.Count} 条日志", Color.Gray);
        }

        private void AddLog(string message, Color color)
        {
            if (lstLog == null) return;
            
            var time = DateTime.Now.ToString("HH:mm:ss");
            lstLog.Items.Insert(0, $"[{time}] {message}");
            
            if (lstLog.Items.Count > 500)
            {
                lstLog.Items.RemoveAt(lstLog.Items.Count - 1);
            }
        }

        private void UpdateStatusCounts()
        {
            if (_monitorService != null)
            {
                if (lblProcessedCount != null)
                {
                    lblProcessedCount.Text = $"已处理: {_monitorService.GetProcessedCount()} 个文件";
                }
                
                if (lblFailedCount != null)
                {
                    lblFailedCount.Text = $"失败: {_monitorService.GetFailedCount()} 个文件";
                }
            }
        }

        private void UpdateProcessedCount()
        {
            UpdateStatusCounts();
        }

        private void BtnRetryFailed_Click(object sender, EventArgs e)
        {
            if (_monitorService == null) return;

            var result = MessageBox.Show(
                "确定要重试所有打印失败的文件吗？",
                "确认重试",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                _monitorService.RetryFailedFiles();
                btnRetryFailed!.Enabled = false;
                AddLog("✓ 已将失败文件重新加入打印队列", Color.Blue);
                UpdateStatusCounts();
            }
        }

        private void BtnClearHistory_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "确定要清除所有历史记录吗？\n\n包括已处理和失败的文件记录，所有文件将被重新检测。",
                "确认清除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                _monitorService?.ClearProcessedHistory();
                AddLog("✓ 历史记录已清除", Color.Orange);
                UpdateStatusCounts();
                if (btnRetryFailed != null)
                {
                    btnRetryFailed.Enabled = false;
                }
            }
        }

        private void CheckPdfPrintTools()
        {
            try
            {
                var info = PdfPrinter.GetAvailablePrintersInfo();
                
                if (info.Contains("✗ 未安装"))
                {
                    AddLog("⚠ 未检测到 PDF 打印工具", Color.Orange);
                    AddLog("   建议安装 SumatraPDF 或 Adobe Reader", Color.Orange);
                    
                    if (btnCheckPdfTools != null)
                    {
                        btnCheckPdfTools.BackColor = Color.FromArgb(255, 193, 7);
                        btnCheckPdfTools.ForeColor = Color.White;
                    }
                }
                else
                {
                    AddLog("✓ PDF 打印工具检测正常", Color.Green);
                    
                    if (btnCheckPdfTools != null)
                    {
                        btnCheckPdfTools.BackColor = Color.FromArgb(40, 167, 69);
                        btnCheckPdfTools.ForeColor = Color.White;
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"检查 PDF 工具时出错: {ex.Message}", Color.Red);
            }
        }

        private void BtnCheckPdfTools_Click(object sender, EventArgs e)
        {
            var info = PdfPrinter.GetAvailablePrintersInfo();
            
            MessageBox.Show(
                info,
                "PDF 打印工具状态",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            
            CheckPdfPrintTools();
        }

        private void BtnBrowseSumatra_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "选择 SumatraPDF.exe 文件",
                Filter = "可执行文件 (*.exe)|*.exe",
                FileName = "SumatraPDF.exe",
                InitialDirectory = @"C:\Program Files"
            };

            if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dialog.FileName))
            {
                if (Path.GetFileName(dialog.FileName).Equals("SumatraPDF.exe", StringComparison.OrdinalIgnoreCase))
                {
                    PdfPrinter.SetCustomSumatraPath(dialog.FileName);
                    AddLog($"✓ 已设置 PDF 工具: {dialog.FileName}", Color.Green);
                    CheckPdfPrintTools();
                    
                    MessageBox.Show(
                        $"已设置 SumatraPDF 路径:\n\n{dialog.FileName}\n\n设置已保存，下次启动自动加载。",
                        "成功",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        "请选择 SumatraPDF.exe 文件！",
                        "错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        private void ChkAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAutoStart == null) return;

            try
            {
                var exePath = Application.ExecutablePath;

                if (chkAutoStart.Checked)
                {
                    AutoStartManager.EnableAutoStart(exePath);
                    AddLog("✓ 已启用开机自启动", Color.Green);
                    
                    MessageBox.Show(
                        "已启用开机自启动！\n\n下次开机时程序将在系统托盘中运行。",
                        "成功",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    AutoStartManager.DisableAutoStart();
                    AddLog("✓ 已禁用开机自启动", Color.Orange);
                }
            }
            catch (Exception ex)
            {
                AddLog($"设置开机自启动失败: {ex.Message}", Color.Red);
                
                if (chkAutoStart.Checked)
                {
                    chkAutoStart.Checked = false;
                    MessageBox.Show(
                        $"启用开机自启动失败:\n{ex.Message}\n\n可能原因：权限不足",
                        "错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        private void CmbPrintScale_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbPrintScale == null) return;

            try
            {
                PdfPrinter.PrintScaleMode mode;
                
                switch (cmbPrintScale.SelectedIndex)
                {
                    case 0:
                        mode = PdfPrinter.PrintScaleMode.FitToPage;
                        AddLog("✓ 打印模式: 适应页面（自动缩放填满纸张）", Color.Green);
                        break;
                    case 1:
                        mode = PdfPrinter.PrintScaleMode.ShrinkToFit;
                        AddLog("✓ 打印模式: 缩小适应（只缩小不放大）", Color.Blue);
                        break;
                    case 2:
                    default:
                        mode = PdfPrinter.PrintScaleMode.None;
                        AddLog("✓ 打印模式: 实际大小（不缩放）", Color.Orange);
                        break;
                }

                PdfPrinter.SetPrintScaleMode(mode);
            }
            catch (Exception ex)
            {
                AddLog($"设置打印模式失败: {ex.Message}", Color.Red);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                var result = MessageBox.Show(
                    "确定要退出吗？\n\n点击【否】可最小化到系统托盘继续运行。",
                    "确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2
                );

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    HideToTray();
                    return;
                }

                if (_monitorService?.IsMonitoring == true)
                {
                    var confirmResult = MessageBox.Show(
                        "监控正在运行中，确定要退出吗？",
                        "确认退出",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (confirmResult == DialogResult.No)
                    {
                        e.Cancel = true;
                        HideToTray();
                        return;
                    }
                    
                    _monitorService.StopMonitoring();
                }
            }
            else
            {
                if (_monitorService?.IsMonitoring == true)
                {
                    _monitorService.StopMonitoring();
                }
            }
            
            notifyIcon?.Dispose();
            trayMenu?.Dispose();
        }
    }
}
