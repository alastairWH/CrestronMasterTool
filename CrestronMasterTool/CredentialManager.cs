using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CrestronMasterTool
{
    public static class CredentialManager
    {
        private static readonly string credFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrestronMasterTool", "usercred.dat");

        public static void SaveCredentials(string username, string password)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(credFile)!);
            var userBytes = Encoding.UTF8.GetBytes(username);
            var passBytes = Encoding.UTF8.GetBytes(password);
            var encryptedPass = ProtectedData.Protect(passBytes, null, DataProtectionScope.CurrentUser);
            using var fs = new FileStream(credFile, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);
            bw.Write(userBytes.Length);
            bw.Write(userBytes);
            bw.Write(encryptedPass.Length);
            bw.Write(encryptedPass);
        }

        public static (string? username, string? password) LoadCredentials()
        {
            if (!File.Exists(credFile)) return (null, null);
            using (var fs = new FileStream(credFile, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                int userLen = br.ReadInt32();
                var userBytes = br.ReadBytes(userLen);
                int passLen = br.ReadInt32();
                var encryptedPass = br.ReadBytes(passLen);
                var passBytes = ProtectedData.Unprotect(encryptedPass, null, DataProtectionScope.CurrentUser);
                return (Encoding.UTF8.GetString(userBytes), Encoding.UTF8.GetString(passBytes));
            }
        }

        public static void ClearCredentials()
        {
            if (File.Exists(credFile))
                File.Delete(credFile);
        }
    }
}
