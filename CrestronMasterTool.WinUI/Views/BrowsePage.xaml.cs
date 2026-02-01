using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CrestronMasterTool.Core.Models;
using CrestronMasterTool.Core.Services;

namespace CrestronMasterTool.WinUI.Views;

public sealed partial class BrowsePage : Page
{
    private CrestronSftpClient? client;
    private readonly ObservableCollection<ProductRow> products = new();
    private readonly ObservableCollection<ProductRow> filtered = new();
    private CancellationTokenSource? downloadCts;

    public BrowsePage()
    {
        InitializeComponent();

        TypeSelector.Items.Add("Software");
        TypeSelector.Items.Add("Firmware");
        TypeSelector.SelectedIndex = 0;

        ProductsList.ItemsSource = filtered;
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        client = e.Parameter as CrestronSftpClient;
        if (client is null)
        {
            FooterStatus.Text = "Missing session.";
            return;
        }

        await LoadProductsAsync();
    }

    private ProductType CurrentType => TypeSelector.SelectedIndex == 1 ? ProductType.Firmware : ProductType.Software;

    private async Task LoadProductsAsync()
    {
        if (client is null) return;

        FooterStatus.Text = "Loading products…";
        products.Clear();
        filtered.Clear();

        try
        {
            var list = await client.ListProductsAsync(CurrentType);
            foreach (var p in list)
            {
                products.Add(new ProductRow(p.DisplayName, p.RemoteFolderName));
            }

            ApplyFilter();
            FooterStatus.Text = $"Found {products.Count} products.";

            // Preload versions lazily: only when a row gets selected.
        }
        catch (Exception ex)
        {
            FooterStatus.Text = "Failed to load products: " + ex.Message;
        }
    }

    private void ApplyFilter()
    {
        string q = (SearchBox.Text ?? string.Empty).Trim().ToLowerInvariant();

        filtered.Clear();
        foreach (var p in products)
        {
            if (string.IsNullOrEmpty(q) || p.Name.ToLowerInvariant().Contains(q))
                filtered.Add(p);
        }
    }

    private async void OnTypeChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadProductsAsync();
    }

    private void OnSearchChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        ApplyFilter();
    }

    private void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        client?.Dispose();
        Frame.Navigate(typeof(LoginPage));
    }

    private async void OnDownloadClick(object sender, RoutedEventArgs e)
    {
        if (client is null) return;

        var selected = filtered.Where(p => p.IsSelected).ToList();
        if (selected.Count == 0)
        {
            FooterStatus.Text = "Select at least one product.";
            return;
        }

        DownloadButton.IsEnabled = false;
        CancelButton.Visibility = Visibility.Visible;
        downloadCts?.Cancel();
        downloadCts = new CancellationTokenSource();

        try
        {
            // Ensure versions are loaded for all selected items
            foreach (var row in selected)
            {
                if (!row.HasVersions)
                {
                    row.Status = "Loading versions…";
                    var versions = await client.ListVersionsAsync(CurrentType, row.RemoteFolderName, downloadCts.Token);
                    row.SetVersions(versions);
                }

                if (row.SelectedVersion is null)
                {
                    row.Status = "Pick a version";
                }
            }

            var downloadTargets = selected.Where(s => s.SelectedVersion is not null).ToList();
            if (downloadTargets.Count == 0)
            {
                FooterStatus.Text = "Pick versions for selected products.";
                return;
            }

            int completed = 0;
            foreach (var row in downloadTargets)
            {
                downloadCts.Token.ThrowIfCancellationRequested();

                string remote = row.SelectedVersion!.RemotePath;
                string fileName = Path.GetFileName(remote);

                string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                string localFile = Path.Combine(downloadsFolder, fileName);

                row.Status = "Downloading…";
                Progress.Value = 0;

                var progress = new Progress<(int percent, long bytesTransferred, long totalBytes)>(p =>
                {
                    Progress.Value = p.percent;
                    FooterStatus.Text = $"{fileName} • {p.percent}%";
                });

                await client.DownloadFileAsync(remote, localFile, progress, downloadCts.Token);

                row.Status = "Done";
                row.IsSelected = false;
                completed++;
            }

            FooterStatus.Text = $"Downloaded {completed} file(s) to Downloads.";
        }
        catch (OperationCanceledException)
        {
            FooterStatus.Text = "Download cancelled.";
        }
        catch (Exception ex)
        {
            FooterStatus.Text = "Download failed: " + ex.Message;
        }
        finally
        {
            Progress.Value = 0;
            DownloadButton.IsEnabled = true;
            CancelButton.Visibility = Visibility.Collapsed;
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        downloadCts?.Cancel();
        FooterStatus.Text = "Cancelling…";
    }
}

public sealed class ProductRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; }
    public string RemoteFolderName { get; }

    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (value == isSelected) return;
            isSelected = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ProductVersion> Versions { get; } = new();

    private ProductVersion? selectedVersion;
    public ProductVersion? SelectedVersion
    {
        get => selectedVersion;
        set
        {
            if (Equals(value, selectedVersion)) return;
            selectedVersion = value;
            OnPropertyChanged();
        }
    }

    public bool HasVersions => Versions.Count > 0;

    private string status = "";
    public string Status
    {
        get => status;
        set
        {
            if (value == status) return;
            status = value;
            OnPropertyChanged();
        }
    }

    public ProductRow(string name, string remoteFolderName)
    {
        Name = name;
        RemoteFolderName = remoteFolderName;
    }

    public void SetVersions(IReadOnlyList<ProductVersion> versions)
    {
        Versions.Clear();
        foreach (var v in versions) Versions.Add(v);

        OnPropertyChanged(nameof(HasVersions));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
