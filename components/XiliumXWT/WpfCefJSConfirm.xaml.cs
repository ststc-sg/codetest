using System.Windows;

namespace XiliumXWT
{
    /// <summary>
    ///     Interaction logic for WpfCefJSConfirm.xaml
    /// </summary>
    public partial class WpfCefJSConfirm : Window
    {
        public WpfCefJSConfirm(string message)
        {
            InitializeComponent();
            messageTextBlock.Text = message;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}