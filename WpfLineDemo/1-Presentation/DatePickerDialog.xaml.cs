using System;
using System.Windows;
using System.Windows.Input;

namespace WpfLineDemo
{
    /// <summary>
    /// Modal date picker for a node's date attribute (Ctrl+D).
    /// OK commits the chosen date, "Odebrat" (only when a date is already set) clears it,
    /// Esc/Zrušit closes without change. On OK/Remove DialogResult is true and
    /// <see cref="SelectedDate"/> holds the result (null when removed).
    /// </summary>
    public partial class DatePickerDialog : Window
    {
        /// <summary>Result after OK/Remove: the chosen date, or null when the date was removed.</summary>
        public DateOnly? SelectedDate { get; private set; }

        public DatePickerDialog(DateOnly? current)
        {
            InitializeComponent();

            // Preselect the existing date, else today.
            DateOnly preselect = current ?? DateOnly.FromDateTime(DateTime.Today);
            Picker.SelectedDate = preselect.ToDateTime(TimeOnly.MinValue);

            // "Odebrat" makes sense only when the node already has a date.
            RemoveButton.Visibility = current.HasValue ? Visibility.Visible : Visibility.Collapsed;

            Loaded += (_, _) => Picker.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedDate = Picker.SelectedDate.HasValue
                ? DateOnly.FromDateTime(Picker.SelectedDate.Value)
                : (DateOnly?)null; // an empty picker on OK acts as "no date"
            DialogResult = true;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedDate = null;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }
        }
    }
}
