using CrestronMasterTool.Core.Credentials;
using CrestronMasterTool.Core.Services;

namespace CrestronMasterTool.WinUI.Views;

public sealed partial class LoginPage : Page
{
    private readonly CredentialStore credentialStore = new();

    public LoginPage()
    {
        InitializeComponent();

        var (savedUser, savedPass) = credentialStore.Load();
        if (!string.IsNullOrWhiteSpace(savedUser)) UsernameBox.Text = savedUser;
        if (!string.IsNullOrWhiteSpace(savedPass)) PasswordBox.Password = savedPass;
        RememberMeCheck.IsChecked = !string.IsNullOrWhiteSpace(savedUser) && !string.IsNullOrWhiteSpace(savedPass);
    }

    private async void OnLoginClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "";
        LoginButton.IsEnabled = false;

        string username = UsernameBox.Text.Trim();
        string password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            StatusText.Text = "Enter your username and password.";
            LoginButton.IsEnabled = true;
            return;
        }

        try
        {
            var client = new CrestronSftpClient();
            await client.ConnectAsync("ftp.crestron.com", 22, username, password);

            if (RememberMeCheck.IsChecked == true)
            {
                credentialStore.Save(username, password);
            }
            else
            {
                credentialStore.Clear();
            }

            Frame.Navigate(typeof(BrowsePage), client);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Login failed: " + ex.Message;
            LoginButton.IsEnabled = true;
        }
    }
}
