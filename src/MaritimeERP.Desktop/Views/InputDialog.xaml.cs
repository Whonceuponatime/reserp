using System.Windows;

namespace MaritimeERP.Desktop.Views
{
    public partial class InputDialog : Window
    {
        public string InputText { get; private set; } = string.Empty;

        public InputDialog(string prompt, string title = "Input Required", string defaultText = "")
        {
            InitializeComponent();
            
            Title = title;
            PromptTextBlock.Text = prompt;
            InputTextBox.Text = defaultText;
            
            // Focus on the text box
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            InputText = InputTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public static string? ShowDialog(string prompt, string title = "Input Required", string defaultText = "")
        {
            var dialog = new InputDialog(prompt, title, defaultText);
            return dialog.ShowDialog() == true ? dialog.InputText : null;
        }
    }
} 