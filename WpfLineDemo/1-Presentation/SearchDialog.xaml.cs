using System.Windows;

namespace WpfLineDemo
{
    /// <summary>
    /// Modal search input (Ctrl+F). Pre-fills the previous term (selected, so it can be overtyped).
    /// OK/Enter returns the text via <see cref="SearchText"/> with DialogResult true; Zrušit/Esc → false.
    /// Empty-text handling is done by the caller (a message + no search).
    /// </summary>
    public partial class SearchDialog : Window
    {
        public string SearchText { get; private set; } = "";

        public SearchDialog(string initial)
        {
            InitializeComponent();
            Input.Text = initial;
            Loaded += (_, _) =>
            {
                Input.Focus();
                Input.SelectAll(); // let the user overtype the pre-filled term straight away
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SearchText = Input.Text;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
