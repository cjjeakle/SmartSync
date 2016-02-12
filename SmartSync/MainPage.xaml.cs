using Windows.ApplicationModel.Email;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SmartSync
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Opens the Windows mail app by creating an empty email composition window.
        /// </summary>
        /// <param name="sender">The event's sender.</param>
        /// <param name="e">Any arguments accompanying the event.</param>
        private void OpenMailApp(object sender, RoutedEventArgs e)
        {
            EmailManager.ShowComposeNewEmailAsync(new EmailMessage());
        }
    }
}
