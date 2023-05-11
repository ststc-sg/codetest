using System.Windows;

namespace XiliumXWT
{
    /// <summary>
    ///     Interaction logic for WpfCefJSAlert.xaml
    /// </summary>
    public partial class WpfCefJSAlert : Window
    {
        public WpfCefJSAlert(string message)
        {
            InitializeComponent();
            messageTextBlock.Text = message;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}