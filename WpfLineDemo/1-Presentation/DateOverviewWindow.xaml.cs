using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MindMap._2_Logical;
using MindMap.Logical;

namespace WpfLineDemo
{
    /// <summary>
    /// Interaction logic for DateOverviewWindow.xaml.
    /// Modal (Ctrl+F2) one-time snapshot of every dated element across the whole hierarchy,
    /// sorted ascending by date. Past dates are gray + italic. Double-click a row navigates
    /// to that element's level and selects it.
    /// </summary>
    public partial class DateOverviewWindow : Window
    {
        // Fixed Czech weekday abbreviations (culture would give lowercase / different forms).
        private static readonly Dictionary<DayOfWeek, string> WeekdayAbbr = new()
        {
            [DayOfWeek.Monday] = "Po",
            [DayOfWeek.Tuesday] = "Út",
            [DayOfWeek.Wednesday] = "St",
            [DayOfWeek.Thursday] = "Čt",
            [DayOfWeek.Friday] = "Pá",
            [DayOfWeek.Saturday] = "So",
            [DayOfWeek.Sunday] = "Ne",
        };

        public DateOverviewWindow()
        {
            InitializeComponent();
            BuildList();
        }

        private void BuildList()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            List<Row> rows = Context.Controller.CollectDatedElements()
                .OrderBy(d => d.Date)
                .ThenBy(d => d.Text)
                .Select(d => new Row
                {
                    DateText = d.Date.ToString("dd.MM.yyyy"),
                    WeekdayText = WeekdayAbbr[d.Date.DayOfWeek],
                    Text = Controller.TruncateLabel(d.Text, 50),
                    IsPast = d.Date < today, // strictly before today; today stays normal
                    Info = d
                })
                .ToList();

            List.ItemsSource = rows;

            if (rows.Count == 0)
            {
                EmptyHint.Text = "Žádný uzel nemá datum.";
            }
        }

        private void List_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (List.SelectedItem is Row row)
            {
                Close();
                Context.Controller.NavigateToElementInLevel(row.Info.OwnerPath, row.Info.Element);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            if (e.Key == Key.Escape || (ctrl && e.Key == Key.F2))
            {
                Close();
                e.Handled = true;
            }
        }

        private class Row
        {
            public string DateText { get; set; } = "";
            public string WeekdayText { get; set; } = "";
            public string Text { get; set; } = "";
            public bool IsPast { get; set; }
            public DatedElementInfo Info { get; set; } = null!;
        }
    }
}
