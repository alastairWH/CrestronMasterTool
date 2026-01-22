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
        private Panel loginPanel;
        private Label lblTitle, lblHost, lblUsername, lblPassword, lblStatus;
        private TextBox txtHost, txtUsername, txtPassword;
        private Button btnLogin;
        private PictureBox logoBox;
        private Panel mainPanel;
        private SftpClient? sftpClient;
        private RadioButton rbSoftware, rbFirmware;
        private ComboBox cmbProduct, cmbVersion;
        private Label lblProduct, lblVersion;
        private Button btnDownload, btnInstall, btnBack;
        private ProgressBar downloadProgress;
        private Label lblMainStatus;
        private Dictionary<string, List<string>> productFiles = new Dictionary<string, List<string>>();

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Crestron Master Tool";
            this.Width = 480;
            this.Height = 340;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            loginPanel = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.WhiteSmoke };

            logoBox = new PictureBox
            {
                Left = 180,
                Top = 18,
                Width = 100,
                Height = 50,
                SizeMode = PictureBoxSizeMode.Zoom,
                // Placeholder: logoBox.Image = ...
                BackColor = System.Drawing.Color.LightGray
            };

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

            loginPanel.Controls.Add(logoBox);
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

            downloadProgress = new ProgressBar { Left = 30, Top = 210, Width = 390, Height = 20, Minimum = 0, Maximum = 100, Value = 0 };

            lblMainStatus = new Label
            {
                Text = "",
                Left = 30,
                Top = 240,
                Width = 390,
                Height = 40,
                ForeColor = System.Drawing.Color.DarkBlue,
                Font = new System.Drawing.Font("Segoe UI", 9)
            };

            btnBack = new Button
            {
                Text = "← Logout",
                Left = 30,
                Top = 285,
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
            mainPanel.Controls.Add(downloadProgress);
            mainPanel.Controls.Add(lblMainStatus);
            mainPanel.Controls.Add(btnBack);

            this.Controls.Add(mainPanel);
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHost.Text))
            {
                lblStatus.Text = "Please enter the SFTP host (e.g. sftp://ftp.crestron.com).";
                return;
            }

            btnLogin.Enabled = false;
            lblStatus.ForeColor = System.Drawing.Color.Black;
            lblStatus.Text = "Connecting...";

            string hostInput = txtHost.Text.Trim();
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

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

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
                        lblStatus.ForeColor = System.Drawing.Color.DarkRed;
                        lblStatus.Text = "Connection failed: " + ex.Message;
                        btnLogin.Enabled = true;
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
            lblStatus.Text = string.Empty;
            loginPanel.Visible = false;
            mainPanel.Visible = true;

            await LoadProductListAsync();
            lblMainStatus.Text = "Connected to " + host + ". Select a product to continue.";
            btnLogin.Enabled = true;
        }

        private async Task LoadProductListAsync()
        {
            cmbProduct.Items.Clear();
            cmbVersion.Items.Clear();
            productFiles.Clear();

            string folderPath = rbSoftware.Checked ? "/software" : "/firmware";
            lblMainStatus.Text = "Loading products...";

            try
            {
                var entries = await Task.Run(() => sftpClient!.ListDirectory(folderPath));
                var products = entries.Where(e => e.IsDirectory && e.Name != "." && e.Name != "..").OrderBy(e => e.Name).ToList();

                foreach (var product in products)
                {
                    cmbProduct.Items.Add(product.Name);
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

        private async void RbType_CheckedChanged(object? sender, EventArgs e)
        {
            if (sftpClient != null && sftpClient.IsConnected && ((RadioButton)sender!).Checked)
            {
                await LoadProductListAsync();
            }
        }

        private async void CmbProduct_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbProduct.SelectedItem == null) return;

            cmbVersion.Items.Clear();
            string productName = cmbProduct.SelectedItem.ToString()!;
            string folderPath = (rbSoftware.Checked ? "/software/" : "/firmware/") + productName;

            lblMainStatus.Text = "Loading versions for " + productName + "...";

            try
            {
                var entries = await Task.Run(() => sftpClient!.ListDirectory(folderPath));
                var files = entries.Where(e => !e.IsDirectory && (e.Name.EndsWith(".exe") || e.Name.EndsWith(".bin")))
                                   .OrderByDescending(e => e.Name)
                                   .ToList();

                if (!productFiles.ContainsKey(productName))
                    productFiles[productName] = new List<string>();

                productFiles[productName].Clear();

                foreach (var file in files)
                {
                    productFiles[productName].Add(file.FullName);
                    string version = ExtractVersion(file.Name);
                    cmbVersion.Items.Add(version + " (" + file.Name + ")");
                }

                if (cmbVersion.Items.Count > 0)
                {
                    cmbVersion.SelectedIndex = 0;
                    lblMainStatus.Text = $"Found {cmbVersion.Items.Count} versions for {productName}.";
                }
                else
                {
                    lblMainStatus.Text = "No files found for " + productName;
                }
            }
            catch (Exception ex)
            {
                lblMainStatus.Text = "Failed to load versions: " + ex.Message;
            }
        }

        private string ExtractVersion(string filename)
        {
            // Extract version pattern like 228.35.001.00 from crestron_database_228.35.001.00.exe
            var match = System.Text.RegularExpressions.Regex.Match(filename, @"(\d+\.\d+\.\d+\.\d+)");
            if (match.Success)
                return match.Groups[1].Value;

            // Fallback: try to extract any version-like pattern
            match = System.Text.RegularExpressions.Regex.Match(filename, @"(\d+[\.\-_]\d+[\.\-_]\d+)");
            if (match.Success)
                return match.Groups[1].Value.Replace('_', '.').Replace('-', '.');

            return filename.Replace(".exe", "").Replace(".bin", "");
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
                lblMainStatus.Text = "Not connected.";
                return;
            }

            if (cmbProduct.SelectedItem == null || cmbVersion.SelectedIndex < 0)
            {
                lblMainStatus.Text = "Please select a product and version.";
                return;
            }

            string productName = cmbProduct.SelectedItem.ToString()!;
            string remotePath = productFiles[productName][cmbVersion.SelectedIndex];
            string fileName = Path.GetFileName(remotePath);
            string localFile = Path.Combine(Path.GetTempPath(), fileName);

            btnDownload.Enabled = false;
            btnInstall.Enabled = false;
            downloadProgress.Value = 0;

            lblMainStatus.Text = "Downloading " + fileName + " ...";

            try
            {
                await Task.Run(() =>
                {
                    using var remote = sftpClient.OpenRead(remotePath);
                    using var local = File.OpenWrite(localFile);
                    var buffer = new byte[64 * 1024];
                    ulong totalRead = 0;
                    int read;
                    while ((read = remote.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        local.Write(buffer, 0, read);
                        totalRead += (ulong)read;
                        if (remote.Length > 0)
                        {
                            int percent = (int)(totalRead * 100 / (ulong)remote.Length);
                            this.Invoke(() => downloadProgress.Value = Math.Min(100, percent));
                        }
                    }
                });

                downloadProgress.Value = 100;
                lblMainStatus.Text = "✓ Downloaded to: " + localFile;
            }
            catch (Exception ex)
            {
                lblMainStatus.Text = "Download failed: " + ex.Message;
            }
            finally
            {
                btnDownload.Enabled = true;
                btnInstall.Enabled = true;
            }
        }

        private void BtnInstall_Click(object? sender, EventArgs e)
        {
            if (cmbProduct.SelectedItem == null || cmbVersion.SelectedIndex < 0)
            {
                lblMainStatus.Text = "Please select a product and version first.";
                return;
            }

            string productName = cmbProduct.SelectedItem.ToString()!;
            string remotePath = productFiles[productName][cmbVersion.SelectedIndex];
            string fileName = Path.GetFileName(remotePath);
            string localFile = Path.Combine(Path.GetTempPath(), fileName);

            if (!File.Exists(localFile))
            {
                lblMainStatus.Text = "File not downloaded yet. Please download first.";
                return;
            }

            lblMainStatus.Text = "Starting silent install for " + fileName + " ...";

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
            mainPanel.Visible = false;
            loginPanel.Visible = true;
        }
    }
}
