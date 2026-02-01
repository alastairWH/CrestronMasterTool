using System.Text.RegularExpressions;
using CrestronMasterTool.Core.Models;
using Renci.SshNet;

namespace CrestronMasterTool.Core.Services;

public sealed class CrestronSftpClient : IDisposable
{
    private SftpClient? sftpClient;

    public bool IsConnected => sftpClient?.IsConnected ?? false;

    public Task ConnectAsync(string host, int port, string username, string password, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            sftpClient?.Dispose();
            sftpClient = new SftpClient(host, port, username, password);
            sftpClient.ConnectionInfo.Timeout = timeout ?? TimeSpan.FromSeconds(10);
            sftpClient.Connect();
        }, cancellationToken);
    }

    public Task<IReadOnlyList<ProductEntry>> ListProductsAsync(ProductType type, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        string basePath = type == ProductType.Software ? "/software" : "/firmware";

        return Task.Run<IReadOnlyList<ProductEntry>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entries = sftpClient!.ListDirectory(basePath);
            var products = entries
                .Where(e => e.IsDirectory && e.Name is not ("." or ".."))
                .OrderBy(e => e.Name)
                .Select(e => new ProductEntry(FormatProductName(e.Name), e.Name))
                .ToList();

            return products;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<ProductVersion>> ListVersionsAsync(ProductType type, string remoteFolderName, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        string basePath = type == ProductType.Software ? "/software" : "/firmware";
        string folderPath = basePath + "/" + remoteFolderName;

        return Task.Run<IReadOnlyList<ProductVersion>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entries = sftpClient!.ListDirectory(folderPath);
            var files = entries
                .Where(e => !e.IsDirectory && (e.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                                              || e.Name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase)
                                              || e.Name.EndsWith(".puf", StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(e => e.Name)
                .Select(e => new ProductVersion(ExtractVersion(e.Name), e.FullName))
                .ToList();

            return files;
        }, cancellationToken);
    }

    public async Task DownloadFileAsync(
        string remotePath,
        string localFile,
        IProgress<(int percent, long bytesTransferred, long totalBytes)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            Directory.CreateDirectory(Path.GetDirectoryName(localFile)!);

            using var remote = sftpClient!.OpenRead(remotePath);
            using var local = File.Open(localFile, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[64 * 1024];
            long totalBytes = remote.Length;
            long transferred = 0;

            int read;
            while ((read = remote.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                local.Write(buffer, 0, read);
                transferred += read;

                if (totalBytes > 0)
                {
                    int percent = (int)Math.Min(100, transferred * 100 / totalBytes);
                    progress?.Report((percent, transferred, totalBytes));
                }
                else
                {
                    progress?.Report((0, transferred, totalBytes));
                }
            }

            progress?.Report((100, transferred, totalBytes));
        }, cancellationToken);
    }

    public void Disconnect()
    {
        if (sftpClient is null) return;

        try
        {
            if (sftpClient.IsConnected) sftpClient.Disconnect();
        }
        finally
        {
            sftpClient.Dispose();
            sftpClient = null;
        }
    }

    public void Dispose() => Disconnect();

    private void EnsureConnected()
    {
        if (sftpClient is null || !sftpClient.IsConnected)
            throw new InvalidOperationException("Not connected.");
    }

    private static string FormatProductName(string name)
    {
        string formatted = name.Replace('_', ' ');
        var words = formatted.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = 0; i < words.Length; i++)
        {
            var w = words[i];
            words[i] = w.Length == 1 ? w.ToUpperInvariant() : char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant();
        }

        return string.Join(" ", words);
    }

    private static string ExtractVersion(string filename)
    {
        var match = Regex.Match(filename, @"(\d+\.\d+\.\d+\.\d+)");
        if (match.Success) return match.Groups[1].Value;

        match = Regex.Match(filename, @"(\d+\.\d+\.\d+)");
        if (match.Success) return match.Groups[1].Value;

        match = Regex.Match(filename, @"(\d+[\.\-_]\d+[\.\-_]\d+)");
        if (match.Success) return match.Groups[1].Value.Replace('_', '.').Replace('-', '.');

        return filename
            .Replace(".exe", "", StringComparison.OrdinalIgnoreCase)
            .Replace(".bin", "", StringComparison.OrdinalIgnoreCase)
            .Replace(".puf", "", StringComparison.OrdinalIgnoreCase);
    }
}
