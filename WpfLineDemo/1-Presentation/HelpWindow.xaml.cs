using System.Windows;
using System.Windows.Input;

namespace WpfLineDemo
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// Modální okno s přehledem klávesových zkratek a ovládání aplikace.
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.F1)
            {
                Close();
                e.Handled = true;
            }
        }
    }
}
