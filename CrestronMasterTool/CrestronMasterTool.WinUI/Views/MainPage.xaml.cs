namespace CrestronMasterTool.WinUI.Views
{
    /// <summary>
    /// A simple page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Loaded += (_, _) => Frame.Navigate(typeof(LoginPage));
        }
    }
}
