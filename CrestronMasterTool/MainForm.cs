using System;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace CrestronMasterTool
{
    public class MainForm : Form
    {
        private Panel? loginPanel;
        private Label? lblTitle, lblHost, lblUsername, lblPassword, lblStatus;
        private TextBox? txtHost, txtUsername, txtPassword;
        private Button? btnLogin;
        private Panel? mainPanel;
        private SftpClient? sftpClient;
        private RadioButton? rbSoftware, rbFirmware;
        private ComboBox? cmbProduct, cmbVersion;
        private Label? lblProduct, lblVersion;
        private Button? btnDownload, btnInstall, btnBack, btnCancel;
        private ProgressBar? downloadProgress;
        private Label? lblMainStatus, lblFileSize;
        private System.Threading.CancellationTokenSource? cancelTokenSource;
        private Dictionary<string, List<string>> productFiles = new Dictionary<string, List<string>>();
        private Dictionary<string, string> productDisplayNames = new Dictionary<string, string>();

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Crestron Master Tool";
            this.Width = 500;
            this.Height = 380;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            loginPanel = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.WhiteSmoke };

            lblTitle = new Label
            {
                Text = "Crestron Master Tool",
                Left = 120,
                Top = 75,
                Width = 250,
                Font = new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            lblHost = new Label { Text = "SFTP Host:", Left = 60, Top = 120, Width = 80, Visible = false };
            txtHost = new TextBox { Left = 150, Top = 118, Width = 220, Text = "sftp://ftp.crestron.com", ReadOnly = true, BackColor = System.Drawing.SystemColors.ControlLight, Visible = false };

            lblUsername = new Label { Text = "Username:", Left = 60, Top = 120, Width = 80 };
            txtUsername = new TextBox { Left = 150, Top = 118, Width = 220 };
            try { txtUsername.PlaceholderText = "Your username"; } catch { }

            lblPassword = new Label { Text = "Password:", Left = 60, Top = 155, Width = 80 };
            txtPassword = new TextBox { Left = 150, Top = 153, Width = 220, PasswordChar = '*' };
            try { txtPassword.PlaceholderText = "Your password"; } catch { }

            btnLogin = new Button
            {
                Text = "Login",
                Left = 150,
                Top = 195,
                Width = 220,
                Height = 32,
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLogin.Click += BtnLogin_Click;

            lblStatus = new Label
            {
                Text = "",
                Left = 60,
                Top = 235,
                Width = 310,
                ForeColor = System.Drawing.Color.DarkRed,
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Italic)
            };

            loginPanel.Controls.Add(lblTitle);
            loginPanel.Controls.Add(lblHost);
            loginPanel.Controls.Add(txtHost);
            loginPanel.Controls.Add(lblUsername);
            loginPanel.Controls.Add(txtUsername);
            loginPanel.Controls.Add(lblPassword);
            loginPanel.Controls.Add(txtPassword);
            loginPanel.Controls.Add(btnLogin);
            loginPanel.Controls.Add(lblStatus);

            this.Controls.Add(loginPanel);

            // MAIN PANEL (hidden until login)
            mainPanel = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = System.Drawing.Color.White };

            var lblTypeTitle = new Label
            {
                Text = "Select Type:",
                Left = 30,
                Top = 25,
                Width = 100,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };

            rbSoftware = new RadioButton { Text = "Software", Left = 140, Top = 25, Width = 100, Checked = true };
            rbSoftware.CheckedChanged += RbType_CheckedChanged;

            rbFirmware = new RadioButton { Text = "Firmware", Left = 250, Top = 25, Width = 100 };
            rbFirmware.CheckedChanged += RbType_CheckedChanged;

            lblProduct = new Label { Text = "Product:", Left = 30, Top = 65, Width = 100, Font = new System.Drawing.Font("Segoe UI", 9) };
            cmbProduct = new ComboBox { Left = 140, Top = 63, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProduct.SelectedIndexChanged += CmbProduct_SelectedIndexChanged;

            lblVersion = new Label { Text = "Version:", Left = 30, Top = 105, Width = 100, Font = new System.Drawing.Font("Segoe UI", 9) };
            cmbVersion = new ComboBox { Left = 140, Top = 103, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };

            btnDownload = new Button
            {
                Text = "Download",
                Left = 140,
                Top = 155,
                Width = 130,
                Height = 35,
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            btnDownload.Click += BtnDownload_Click;

            btnInstall = new Button
            {
                Text = "Install (Silent)",
                Left = 280,
                Top = 155,
                Width = 140,
                Height = 35,
                BackColor = System.Drawing.Color.FromArgb(16, 137, 62),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            btnInstall.Click += BtnInstall_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 200,
                Top = 200,
                Width = 80,
                Height = 25,
                BackColor = System.Drawing.Color.FromArgb(200, 0, 0),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9),
                Visible = false
            };
            btnCancel.Click += BtnCancel_Click;

            downloadProgress = new ProgressBar { Left = 30, Top = 240, Width = 390, Height = 20, Minimum = 0, Maximum = 100, Value = 0 };

            lblFileSize = new Label
            {
                Text = "",
                Left = 30,
                Top = 265,
                Width = 390,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font("Segoe UI", 8)
            };

            lblMainStatus = new Label
            {
                Text = "",
                Left = 30,
                Top = 285,
                Width = 390,
                Height = 40,
                ForeColor = System.Drawing.Color.DarkBlue,
                Font = new System.Drawing.Font("Segoe UI", 9)
            };

            btnBack = new Button
            {
                Text = "← Logout",
                Left = 30,
                Top = 330,
                Width = 100,
                Height = 25,
                FlatStyle = FlatStyle.Flat,
                ForeColor = System.Drawing.Color.Gray
            };
            btnBack.Click += BtnBack_Click;

            mainPanel.Controls.Add(lblTypeTitle);
            mainPanel.Controls.Add(rbSoftware);
            mainPanel.Controls.Add(rbFirmware);
            mainPanel.Controls.Add(lblProduct);
            mainPanel.Controls.Add(cmbProduct);
            mainPanel.Controls.Add(lblVersion);
            mainPanel.Controls.Add(cmbVersion);
            mainPanel.Controls.Add(btnDownload);
            mainPanel.Controls.Add(btnInstall);
            mainPanel.Controls.Add(btnCancel);
            mainPanel.Controls.Add(downloadProgress);
            mainPanel.Controls.Add(lblFileSize);
            mainPanel.Controls.Add(lblMainStatus);
            mainPanel.Controls.Add(btnBack);

            this.Controls.Add(mainPanel);
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHost!.Text))
            {
                lblStatus!.Text = "Please enter the SFTP host (e.g. sftp://ftp.crestron.com).";
                return;
            }

            btnLogin!.Enabled = false;
            lblStatus!.ForeColor = System.Drawing.Color.Black;
            lblStatus.Text = "Connecting...";

            string hostInput = txtHost!.Text.Trim();
            int port = 22;
            if (hostInput.StartsWith("sftp://", StringComparison.OrdinalIgnoreCase))
                hostInput = hostInput.Substring("sftp://".Length);

            // allow host:port
            string host = hostInput;
            if (hostInput.Contains(':'))
            {
                var parts = hostInput.Split(':');
                host = parts[0];
                if (int.TryParse(parts[1], out var p)) port = p;
            }

            string username = txtUsername!.Text.Trim();
            string password = txtPassword!.Text;

            await Task.Run(() =>
            {
                try
                {
                    sftpClient?.Dispose();
                    sftpClient = new SftpClient(host, port, username, password);
                    sftpClient.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);
                    sftpClient.Connect();
                }
                catch (Exception ex)
                {
                    sftpClient = null;
                    this.Invoke(() =>
                    {
                        lblStatus!.ForeColor = System.Drawing.Color.DarkRed;
                        lblStatus.Text = "Connection failed: " + ex.Message;
                        btnLogin!.Enabled = true;
                    });
                }
            });

            if (sftpClient == null || !sftpClient.IsConnected)
            {
                if (sftpClient != null) sftpClient.Dispose();
                btnLogin.Enabled = true;
                return;
            }

            // Connected
            lblStatus!.Text = string.Empty;
            loginPanel!.Visible = false;
            mainPanel!.Visible = true;

            await LoadProductListAsync();
            lblMainStatus!.Text = "Connected to " + host + ". Select a product to continue.";
            btnLogin!.Enabled = true;
        }

        private async Task LoadProductListAsync()
        {
            cmbProduct!.Items.Clear();
            cmbVersion!.Items.Clear();
            productFiles.Clear();
            productDisplayNames.Clear();

            string folderPath = rbSoftware!.Checked ? "/software" : "/firmware";
            lblMainStatus!.Text = "Loading products...";

            try
            {
                var entries = await Task.Run(() => sftpClient!.ListDirectory(folderPath));
                var products = entries.Where(e => e.IsDirectory && e.Name != "." && e.Name != "..").OrderBy(e => e.Name).ToList();

                foreach (var product in products)
                {
                    string displayName = FormatProductName(product.Name);
                    productDisplayNames[displayName] = product.Name;
                    cmbProduct.Items.Add(displayName);
                }

                if (cmbProduct.Items.Count > 0)
                {
                    lblMainStatus.Text = $"Found {cmbProduct.Items.Count} products.";
                }
                else
                {
                    lblMainStatus.Text = "No products found in " + folderPath;
                }
            }
            catch (Exception ex)
            {
                lblMainStatus.Text = "Failed to load products: " + ex.Message;
            }
        }

        private string FormatProductName(string name)
        {
            // Replace underscores with spaces
            string formatted = name.Replace('_', ' ');
            
            // Capitalize first letter of each word
            var words = formatted.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (!string.IsNullOrEmpty(words[i]))
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            
            return string.Join(" ", words);
        }

        private async void RbType_CheckedChanged(object? sender, EventArgs e)
        {
            if (sftpClient != null && sftpClient.IsConnected && ((RadioButton)sender!).Checked)
            {
                // Hide Install button for firmware, show for software
                btnInstall!.Visible = rbSoftware!.Checked;
                
                await LoadProductListAsync();
            }
        }

        private async void CmbProduct_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbProduct!.SelectedItem == null) return;

            cmbVersion!.Items.Clear();
            string displayName = cmbProduct.SelectedItem.ToString()!;
            string productName = productDisplayNames[displayName];
            string folderPath = (rbSoftware!.Checked ? "/software/" : "/firmware/") + productName;

            lblMainStatus!.Text = "Loading versions for " + displayName + "...";

            try
            {
                var entries = await Task.Run(() => sftpClient!.ListDirectory(folderPath));
                var files = entries.Where(e => !e.IsDirectory && (e.Name.EndsWith(".exe") || e.Name.EndsWith(".bin") || e.Name.EndsWith(".puf")))
                                   .OrderByDescending(e => e.Name)
                                   .ToList();

                if (!productFiles.ContainsKey(displayName))
                    productFiles[displayName] = new List<string>();

                productFiles[displayName].Clear();

                foreach (var file in files)
                {
                    productFiles[displayName].Add(file.FullName);
                    string version = ExtractVersion(file.Name);
                    cmbVersion.Items.Add(version + " (" + file.Name + ")");
                }

                if (cmbVersion.Items.Count > 0)
                {
                    cmbVersion.SelectedIndex = 0;
                    lblMainStatus.Text = $"Found {cmbVersion.Items.Count} versions for {displayName}.";
                }
                else
                {
                    lblMainStatus.Text = "No files found for " + displayName;
                }
            }
            catch (Exception ex)
            {
                lblMainStatus.Text = "Failed to load versions: " + ex.Message;
            }
        }

        private string ExtractVersion(string filename)
        {
            // Extract version pattern like 228.35.001.00 or 1.8001.0295 from filename
            var match = System.Text.RegularExpressions.Regex.Match(filename, @"(\d+\.\d+\.\d+\.\d+)");
            if (match.Success)
                return match.Groups[1].Value;

            // Try 3-part version like 1.8001.0295
            match = System.Text.RegularExpressions.Regex.Match(filename, @"(\d+\.\d+\.\d+)");
            if (match.Success)
                return match.Groups[1].Value;

            // Fallback: try to extract any version-like pattern
            match = System.Text.RegularExpressions.Regex.Match(filename, @"(\d+[\.\-_]\d+[\.\-_]\d+)");
            if (match.Success)
                return match.Groups[1].Value.Replace('_', '.').Replace('-', '.');

            return filename.Replace(".exe", "").Replace(".bin", "").Replace(".puf", "");
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 0) bytes = 0;
            double b = bytes;
            if (b >= 1L << 30) return (b / (1L << 30)).ToString("0.##") + " GB";
            if (b >= 1L << 20) return (b / (1L << 20)).ToString("0.##") + " MB";
            if (b >= 1L << 10) return (b / (1L << 10)).ToString("0.##") + " KB";
            return b + " B";
        }

        private async void BtnDownload_Click(object? sender, EventArgs e)
        {
            if (sftpClient == null || !sftpClient.IsConnected)
            {
                lblMainStatus!.Text = "Not connected.";
                return;
            }

            if (cmbProduct!.SelectedItem == null || cmbVersion!.SelectedIndex < 0)
            {
                lblMainStatus!.Text = "Please select a product and version.";
                return;
            }

            string productName = cmbProduct.SelectedItem.ToString()!;
            string remotePath = productFiles[productName][cmbVersion!.SelectedIndex];
            string fileName = Path.GetFileName(remotePath);
            string localFile = Path.Combine(Path.GetTempPath(), fileName);

            btnDownload!.Enabled = false;
            btnInstall!.Enabled = false;
            btnCancel!.Visible = true;
            downloadProgress!.Value = 0;
            lblFileSize!.Text = "";

            cancelTokenSource?.Cancel();
            cancelTokenSource = new System.Threading.CancellationTokenSource();
            var token = cancelTokenSource.Token;

            lblMainStatus!.Text = "Downloading " + fileName + " ...";

            try
            {
                var startTime = DateTime.Now;
                await Task.Run(() =>
                {
                    using var remote = sftpClient.OpenRead(remotePath);
                    using var local = File.OpenWrite(localFile);
                    var buffer = new byte[64 * 1024];
                    ulong totalRead = 0;
                    long fileSize = remote.Length;
                    int read;
                    
                    while ((read = remote.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (token.IsCancellationRequested)
                        {
                            local.Close();
                            File.Delete(localFile);
                            return;
                        }

                        local.Write(buffer, 0, read);
                        totalRead += (ulong)read;
                        
                        if (fileSize > 0)
                        {
                            int percent = (int)(totalRead * 100 / (ulong)fileSize);
                            var elapsed = DateTime.Now - startTime;
                            double speed = totalRead / elapsed.TotalSeconds;
                            double remaining = (fileSize - (long)totalRead) / speed;
                            
                            this.Invoke(() =>
                            {
                                downloadProgress.Value = Math.Min(100, percent);
                                lblFileSize.Text = $"{FormatSize((long)totalRead)} / {FormatSize(fileSize)} ({FormatSpeed(speed)}) - ETA: {FormatTime(remaining)}";
                            });
                        }
                    }
                }, token);

                if (!token.IsCancellationRequested)
                {
                    downloadProgress.Value = 100;
                    lblMainStatus.Text = "✓ Downloaded to: " + localFile;
                }
                else
                {
                    lblMainStatus.Text = "Download cancelled.";
                    lblFileSize.Text = "";
                }
            }
            catch (Exception ex)
            {
                lblMainStatus.Text = "Download failed: " + ex.Message;
                lblFileSize.Text = "";
            }
            finally
            {
                btnDownload.Enabled = true;
                btnInstall.Enabled = true;
                btnCancel.Visible = false;
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            cancelTokenSource?.Cancel();
            lblMainStatus!.Text = "Cancelling download...";
        }

        private string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond >= 1 << 20) return (bytesPerSecond / (1 << 20)).ToString("0.##") + " MB/s";
            if (bytesPerSecond >= 1 << 10) return (bytesPerSecond / (1 << 10)).ToString("0.##") + " KB/s";
            return bytesPerSecond.ToString("0") + " B/s";
        }

        private string FormatTime(double seconds)
        {
            if (seconds < 0 || double.IsInfinity(seconds) || double.IsNaN(seconds)) return "--";
            if (seconds > 3600) return $"{(int)(seconds / 3600)}h {(int)((seconds % 3600) / 60)}m";
            if (seconds > 60) return $"{(int)(seconds / 60)}m {(int)(seconds % 60)}s";
            return $"{(int)seconds}s";
        }

        private void BtnInstall_Click(object? sender, EventArgs e)
        {
            if (cmbProduct!.SelectedItem == null || cmbVersion!.SelectedIndex < 0)
            {
                lblMainStatus!.Text = "Please select a product and version first.";
                return;
            }

            string productName = cmbProduct.SelectedItem.ToString()!;
            string remotePath = productFiles[productName][cmbVersion!.SelectedIndex];
            string fileName = Path.GetFileName(remotePath);
            string localFile = Path.Combine(Path.GetTempPath(), fileName);

            if (!File.Exists(localFile))
            {
                lblMainStatus!.Text = "File not downloaded yet. Please download first.";
                return;
            }

            lblMainStatus!.Text = "Starting silent install for " + fileName + " ...";

            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = localFile,
                    Arguments = "/quiet /norestart",
                    UseShellExecute = true,
                    Verb = "runas" // Run as admin
                };

                var process = System.Diagnostics.Process.Start(startInfo);
                lblMainStatus.Text = "✓ Install started for " + fileName;
            }
            catch (Exception ex)
            {
                lblMainStatus.Text = "Install failed: " + ex.Message;
            }
        }

        private void BtnBack_Click(object? sender, EventArgs e)
        {
            mainPanel!.Visible = false;
            loginPanel!.Visible = true;
        }
    }
}
