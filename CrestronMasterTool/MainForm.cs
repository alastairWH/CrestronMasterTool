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
        private TextBox? txtSearch;
        private ListView? lvProducts;
        private Button? btnDownloadSelected, btnInstallSelected, btnCancel;
        private ProgressBar? downloadProgress;
        private Label? lblMainStatus, lblFileSize;
        private System.Threading.CancellationTokenSource? cancelTokenSource;
        private Dictionary<string, List<string>> productFiles = new Dictionary<string, List<string>>();
        private Dictionary<string, string> productDisplayNames = new Dictionary<string, string>();
        private Dictionary<string, Dictionary<string, string>> productVersions = new Dictionary<string, Dictionary<string, string>>();
        private List<string> downloadQueue = new List<string>();
        private List<ListViewItem> allProductItems = new List<ListViewItem>();

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Crestron Master Tool";
            this.Width = 700;
            this.Height = 580;
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

            var lblSearch = new Label { Text = "Search:", Left = 30, Top = 65, Width = 60, Font = new System.Drawing.Font("Segoe UI", 9) };
            txtSearch = new TextBox { Left = 95, Top = 63, Width = 550, Font = new System.Drawing.Font("Segoe UI", 9) };
            txtSearch.TextChanged += TxtSearch_TextChanged;
            try { txtSearch.PlaceholderText = "Type to filter products..."; } catch { }

            lvProducts = new ListView
            {
                Left = 30,
                Top = 95,
                Width = 620,
                Height = 280,
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = true,
                GridLines = true,
                Font = new System.Drawing.Font("Segoe UI", 9)
            };
            lvProducts.Columns.Add("Product", 350);
            lvProducts.Columns.Add("Version", 200);
            lvProducts.Columns.Add("Status", 70);
            lvProducts.ItemChecked += LvProducts_ItemChecked;
            lvProducts.MouseClick += LvProducts_MouseClick;

            btnDownloadSelected = new Button
            {
                Text = "Download Selected",
                Left = 30,
                Top = 390,
                Width = 150,
                Height = 35,
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            btnDownloadSelected.Click += BtnDownloadSelected_Click;

            btnInstallSelected = new Button
            {
                Text = "Install Selected",
                Left = 190,
                Top = 390,
                Width = 150,
                Height = 35,
                BackColor = System.Drawing.Color.FromArgb(16, 137, 62),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            btnInstallSelected.Click += BtnInstallSelected_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 350,
                Top = 390,
                Width = 100,
                Height = 35,
                BackColor = System.Drawing.Color.FromArgb(200, 0, 0),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9),
                Visible = false
            };
            btnCancel.Click += BtnCancel_Click;

            downloadProgress = new ProgressBar { Left = 30, Top = 435, Width = 620, Height = 20, Minimum = 0, Maximum = 100, Value = 0 };

            lblFileSize = new Label
            {
                Text = "",
                Left = 30,
                Top = 460,
                Width = 620,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font("Segoe UI", 8)
            };

            lblMainStatus = new Label
            {
                Text = "",
                Left = 30,
                Top = 460,
                Width = 400,
                Height = 40,
                ForeColor = System.Drawing.Color.DarkBlue,
                Font = new System.Drawing.Font("Segoe UI", 9)
            };

            var btnLogout = new Button
            {
                Text = "Logout",
                Left = 30,
                Top = 505,
                Width = 110,
                Height = 30,
                BackColor = System.Drawing.Color.FromArgb(220, 53, 69),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
            };
            btnLogout.Click += BtnBack_Click;

            mainPanel.Controls.Add(lblTypeTitle);
            mainPanel.Controls.Add(rbSoftware);
            mainPanel.Controls.Add(rbFirmware);
            mainPanel.Controls.Add(lblSearch);
            mainPanel.Controls.Add(txtSearch);
            mainPanel.Controls.Add(lvProducts);
            mainPanel.Controls.Add(btnDownloadSelected);
            mainPanel.Controls.Add(btnInstallSelected);
            mainPanel.Controls.Add(btnCancel);
            mainPanel.Controls.Add(downloadProgress);
            mainPanel.Controls.Add(lblFileSize);
            mainPanel.Controls.Add(lblMainStatus);
            mainPanel.Controls.Add(btnLogout);

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
            lvProducts!.Items.Clear();
            productFiles.Clear();
            productDisplayNames.Clear();
            productVersions.Clear();
            allProductItems.Clear();

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
                    
                    // Don't load versions upfront - do it on demand when user clicks
                    
                    // Add to ListView
                    var item = new ListViewItem(displayName);
                    item.SubItems.Add("(Select version) ▼");
                    item.SubItems.Add("");
                    item.Tag = displayName;
                    allProductItems.Add(item);
                    lvProducts.Items.Add(item);
                }

                if (lvProducts.Items.Count > 0)
                {
                    lblMainStatus.Text = $"Found {lvProducts.Items.Count} products. Check items and select versions to download.";
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

        private async Task LoadVersionsForProduct(string displayName, string productName, string basePath)
        {
            try
            {
                string folderPath = basePath + "/" + productName;
                var entries = await Task.Run(() => sftpClient!.ListDirectory(folderPath));
                var files = entries.Where(e => !e.IsDirectory && (e.Name.EndsWith(".exe") || e.Name.EndsWith(".bin") || e.Name.EndsWith(".puf")))
                                   .OrderByDescending(e => e.Name)
                                   .ToList();

                if (!productFiles.ContainsKey(displayName))
                    productFiles[displayName] = new List<string>();
                
                if (!productVersions.ContainsKey(displayName))
                    productVersions[displayName] = new Dictionary<string, string>();

                productFiles[displayName].Clear();
                productVersions[displayName].Clear();

                foreach (var file in files)
                {
                    productFiles[displayName].Add(file.FullName);
                    string versionDisplay = ExtractVersion(file.Name) + " (" + file.Name + ")";
                    productVersions[displayName][versionDisplay] = file.FullName;
                }
            }
            catch
            {
                // Ignore errors for individual products
            }
        }

        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            string searchText = txtSearch!.Text.ToLower().Trim();
            
            lvProducts!.BeginUpdate();
            lvProducts.Items.Clear();
            
            foreach (ListViewItem item in allProductItems)
            {
                string productName = item.Text.ToLower();
                
                if (string.IsNullOrEmpty(searchText) || productName.Contains(searchText))
                {
                    lvProducts.Items.Add(item);
                }
            }
            
            lvProducts.EndUpdate();
        }

        private void LvProducts_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            // When an item is checked, show a context menu or inline dropdown for version selection
            if (e.Item.Checked && e.Item.SubItems[1].Text.Contains("(Select version)"))
            {
                ShowVersionSelector(e.Item);
            }
        }

        private void LvProducts_MouseClick(object? sender, MouseEventArgs e)
        {
            // When user clicks on the version column, show the dropdown
            var info = lvProducts!.HitTest(e.X, e.Y);
            if (info.Item != null && info.SubItem != null)
            {
                int subItemIndex = info.Item.SubItems.IndexOf(info.SubItem);
                
                // If clicked on the Version column (index 1)
                if (subItemIndex == 1)
                {
                    ShowVersionSelector(info.Item);
                }
            }
        }

        private async void ShowVersionSelector(ListViewItem item)
        {
            string productName = (string)item.Tag!;
            
            // Load versions on demand if not already loaded
            if (!productVersions.ContainsKey(productName) || productVersions[productName].Count == 0)
            {
                string basePath = rbSoftware!.Checked ? "/software" : "/firmware";
                string rawProductName = productDisplayNames[productName];
                
                lblMainStatus!.Text = "Loading versions for " + productName + "...";
                await LoadVersionsForProduct(productName, rawProductName, basePath);
                lblMainStatus.Text = "";
            }
            
            if (!productVersions.ContainsKey(productName) || productVersions[productName].Count == 0)
            {
                item.SubItems[1].Text = "No versions available";
                item.Checked = false;
                return;
            }

            // Create a context menu to select version
            var menu = new ContextMenuStrip();
            
            foreach (var version in productVersions[productName].Keys)
            {
                var menuItem = new ToolStripMenuItem(version);
                menuItem.Click += (s, ev) =>
                {
                    item.SubItems[1].Text = version + " ▼";
                    item.SubItems[2].Text = "Ready";
                };
                menu.Items.Add(menuItem);
            }
            
            // Show the menu at the ListView position
            var rect = item.SubItems[1].Bounds;
            menu.Show(lvProducts!, rect.Left, rect.Bottom);
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
                btnInstallSelected!.Visible = rbSoftware!.Checked;
                
                await LoadProductListAsync();
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

        private async void BtnDownloadSelected_Click(object? sender, EventArgs e)
        {
            if (sftpClient == null || !sftpClient.IsConnected)
            {
                lblMainStatus!.Text = "Not connected.";
                return;
            }

            // Get all checked items with selected versions
            var itemsToDownload = new List<(ListViewItem item, string remotePath, string fileName)>();
            
            foreach (ListViewItem item in lvProducts!.Items)
            {
                if (item.Checked && item.SubItems[1].Text != "(Select version)" && item.SubItems[1].Text != "No versions available")
                {
                    string productName = (string)item.Tag!;
                    string versionDisplay = item.SubItems[1].Text;
                    
                    if (productVersions.ContainsKey(productName) && productVersions[productName].ContainsKey(versionDisplay))
                    {
                        string remotePath = productVersions[productName][versionDisplay];
                        string fileName = Path.GetFileName(remotePath);
                        itemsToDownload.Add((item, remotePath, fileName));
                    }
                }
            }

            if (itemsToDownload.Count == 0)
            {
                lblMainStatus!.Text = "Please check items and select versions to download.";
                return;
            }

            btnDownloadSelected!.Enabled = false;
            btnInstallSelected!.Enabled = false;
            btnCancel!.Visible = true;

            cancelTokenSource?.Cancel();
            cancelTokenSource = new System.Threading.CancellationTokenSource();
            var token = cancelTokenSource.Token;

            lblMainStatus!.Text = $"Downloading {itemsToDownload.Count} file(s)...";

            int completed = 0;
            foreach (var (item, remotePath, fileName) in itemsToDownload)
            {
                if (token.IsCancellationRequested) break;

                item.SubItems[2].Text = "Downloading...";
                item.BackColor = System.Drawing.Color.LightYellow;

                string downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                downloadsFolder = Path.Combine(downloadsFolder, "Downloads");
                if (!Directory.Exists(downloadsFolder))
                    Directory.CreateDirectory(downloadsFolder);
                string localFile = Path.Combine(downloadsFolder, fileName);

                try
                {
                    var startTime = DateTime.Now;
                    await Task.Run(() =>
                    {
                        using var remote = sftpClient!.OpenRead(remotePath);
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
                                    downloadProgress!.Value = Math.Min(100, percent);
                                    lblFileSize!.Text = $"[{completed + 1}/{itemsToDownload.Count}] {fileName}: {FormatSize((long)totalRead)} / {FormatSize(fileSize)} ({FormatSpeed(speed)}) - ETA: {FormatTime(remaining)}";
                                });
                            }
                        }
                    }, token);

                    if (!token.IsCancellationRequested)
                    {
                        item.SubItems[2].Text = "✓ Done";
                        item.BackColor = System.Drawing.Color.LightGreen;
                        completed++;
                    }
                }
                catch (Exception ex)
                {
                    item.SubItems[2].Text = "✗ Failed";
                    item.BackColor = System.Drawing.Color.LightCoral;
                    lblMainStatus.Text = $"Error downloading {fileName}: " + ex.Message;
                }
            }

            downloadProgress!.Value = 0;
            
            if (!token.IsCancellationRequested)
            {
                lblMainStatus!.Text = $"✓ Downloaded {completed} of {itemsToDownload.Count} file(s) to Downloads folder.";
                lblFileSize!.Text = "";
            }
            else
            {
                lblMainStatus!.Text = "Download cancelled.";
                lblFileSize!.Text = "";
            }

            btnDownloadSelected.Enabled = true;
            btnInstallSelected.Enabled = true;
            btnCancel.Visible = false;
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

        private void BtnInstallSelected_Click(object? sender, EventArgs e)
        {
            string downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            downloadsFolder = Path.Combine(downloadsFolder, "Downloads");

            // Get all checked items that have been downloaded
            var itemsToInstall = new List<(string fileName, string localFile)>();
            
            foreach (ListViewItem item in lvProducts!.Items)
            {
                if (item.Checked && item.SubItems[2].Text == "✓ Done")
                {
                    string productName = (string)item.Tag!;
                    string versionDisplay = item.SubItems[1].Text;
                    
                    if (productVersions.ContainsKey(productName) && productVersions[productName].ContainsKey(versionDisplay))
                    {
                        string remotePath = productVersions[productName][versionDisplay];
                        string fileName = Path.GetFileName(remotePath);
                        string localFile = Path.Combine(downloadsFolder, fileName);
                        
                        if (File.Exists(localFile))
                        {
                            itemsToInstall.Add((fileName, localFile));
                        }
                    }
                }
            }

            if (itemsToInstall.Count == 0)
            {
                lblMainStatus!.Text = "No downloaded files selected for installation. Please download first.";
                return;
            }

            lblMainStatus!.Text = $"Starting installation for {itemsToInstall.Count} file(s)...";

            int installed = 0;
            foreach (var (fileName, localFile) in itemsToInstall)
            {
                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = localFile,
                        Arguments = "/quiet /norestart",
                        UseShellExecute = true,
                        Verb = "runas" // Run as admin
                    };

                    System.Diagnostics.Process.Start(startInfo);
                    installed++;
                }
                catch (Exception ex)
                {
                    lblMainStatus.Text = $"Install failed for {fileName}: " + ex.Message;
                    return;
                }
            }

            lblMainStatus.Text = $"✓ Started installation for {installed} file(s).";
        }

        private void BtnBack_Click(object? sender, EventArgs e)
        {
            mainPanel!.Visible = false;
            loginPanel!.Visible = true;
        }
    }
}
