using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CrestronMasterTool
{
    public static class InstalledSoftwareHelper
    {
        // This method checks installed Crestron software and updates the ListView UI with version and update status
        public static void UpdateInstalledSoftwareStatus(ListView lvProducts, Dictionary<string, string> productDisplayNames, Dictionary<string, Dictionary<string, string>> productVersions)
        {
            var manager = new SoftwareManager();
            var installed = manager.GetInstalledSoftware();
            if (installed == null || installed.Count == 0) return;

            foreach (ListViewItem item in lvProducts.Items)
            {
                string displayName = item.Text;
                string? rawName = productDisplayNames.ContainsKey(displayName) ? productDisplayNames[displayName] : null;
                if (rawName == null) continue;

                // Try to find installed software matching this product
                var match = installed.FirstOrDefault(s =>
                    (!string.IsNullOrEmpty(s.Name) && displayName.ToLower().Contains(s.Name.ToLower())) ||
                    (!string.IsNullOrEmpty(s.Name) && s.Name.ToLower().Contains(displayName.ToLower()))
                );

                if (match != null)
                {
                    // Set installed version in Version column if not already set
                    string installedVersion = match.Version ?? "";
                    string currentVersion = item.SubItems[1].Text.Replace(" ▼", "");
                    if (!string.IsNullOrEmpty(installedVersion) && !currentVersion.Contains(installedVersion))
                    {
                        item.SubItems[1].Text = installedVersion + " (Installed)";
                    }

                    // Check if update is available
                    if (productVersions.ContainsKey(displayName))
                    {
                        var availableVersions = productVersions[displayName].Keys.ToList();
                        if (availableVersions.Any(v => !v.Contains(installedVersion)))
                        {
                            item.SubItems[2].Text = "Update available";
                            item.BackColor = System.Drawing.Color.LightGoldenrodYellow;
                        }
                        else
                        {
                            item.SubItems[2].Text = "Up to date";
                            item.BackColor = System.Drawing.Color.LightGreen;
                        }
                    }
                }
            }
        }
    }
}
