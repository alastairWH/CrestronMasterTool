using System.Security.Cryptography;
using System.Text;

namespace CrestronMasterTool.Core.Credentials;

public sealed class CredentialStore
{
    private readonly string credFile;

    public CredentialStore(string? appName = null)
    {
        appName ??= "CrestronMasterTool";
        credFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            appName,
            "usercred.dat");
    }

    public void Save(string username, string password)
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

    public (string? username, string? password) Load()
    {
        if (!File.Exists(credFile)) return (null, null);

        using var fs = new FileStream(credFile, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);
        int userLen = br.ReadInt32();
        var userBytes = br.ReadBytes(userLen);
        int passLen = br.ReadInt32();
        var encryptedPass = br.ReadBytes(passLen);
        var passBytes = ProtectedData.Unprotect(encryptedPass, null, DataProtectionScope.CurrentUser);
        return (Encoding.UTF8.GetString(userBytes), Encoding.UTF8.GetString(passBytes));
    }

    public void Clear()
    {
        if (File.Exists(credFile)) File.Delete(credFile);
    }
}
