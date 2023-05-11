using System.Windows;

namespace XiliumXWT
{
    /// <summary>
    ///     Interaction logic for WpfCefJSPrompt.xaml
    /// </summary>
    public partial class WpfCefJSPrompt : Window
    {
        public WpfCefJSPrompt(string message, string defaultText)
        {
            InitializeComponent();
            messageTextBlock.Text = message;
            inputTextBox.Text = defaultText;
        }

        public string Input
        {
            get { return inputTextBox.Text; }
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