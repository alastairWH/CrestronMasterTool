using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Microsoft.Win32;

namespace CrestronMasterTool
{
    public class SoftwareManager
    {
        // List installed software from registry
        public List<InstalledSoftware> GetInstalledSoftware()
        {
            var result = new List<InstalledSoftware>();
            string[] registryKeys = new string[]
            {
                @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
                @"SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
            };

            foreach (string keyPath in registryKeys)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key == null) continue;
                    foreach (string subkeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkeyName))
                        {
                            string name = subkey.GetValue("DisplayName") as string;
                            string version = subkey.GetValue("DisplayVersion") as string;
                            string publisher = subkey.GetValue("Publisher") as string;
                            string uninstallString = subkey.GetValue("UninstallString") as string;
                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(publisher) && publisher.ToLower().Contains("crestron"))
                            {
                                result.Add(new InstalledSoftware
                                {
                                    Name = name,
                                    Version = version,
                                    Publisher = publisher,
                                    UninstallString = uninstallString,
                                    Status = "Update available"
                                });
                            }
                        }
                    }
                }
            }
            return result;
        }

        // Download a file from a URL
        public bool DownloadFile(string url, string destinationPath)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(url, destinationPath);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Run an installer to update software
        public bool RunInstaller(string installerPath, string arguments = "")
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = installerPath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = true;
                process.Start();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public class InstalledSoftware
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public string UninstallString { get; set; }
        public string Status { get; set; } // e.g. "Update available"
    }
}
