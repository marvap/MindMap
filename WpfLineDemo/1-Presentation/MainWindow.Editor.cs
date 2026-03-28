using MindMap.Logical;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfLineDemo
{
    public partial class MainWindow : Window
    {
        private void ShowNewEditor(Point position)
        {
            _activeEditor = new TextBox
            {
                MinWidth = 120,
                MinHeight = 30,
                AcceptsReturn = true, // víceřádkovost
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                FontSize = 12,
                Padding = new Thickness(6),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.SteelBlue,
                Background = Brushes.White
            };

            _activeEditor.KeyDown += Editor_KeyDown;
            _activeEditor.TextChanged += Editor_TextChanged;
            _activeEditor.LostKeyboardFocus += Editor_LostKeyboardFocus;

            Canvas.SetLeft(_activeEditor, position.X);
            Canvas.SetTop(_activeEditor, position.Y);

            MyCanvas.Children.Add(_activeEditor);
            _activeEditor.Focus();
            _activeEditor.CaretIndex = 0;

            ResizeEditorToContent();
        }

        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            ResizeEditorToContent();
        }

        private void ResizeEditorToContent()
        {
            if (_activeEditor == null)
                return;

            Size desiredSize = MeasureText(_activeEditor.Text, _activeEditor);

            _activeEditor.Width = Math.Max(120, desiredSize.Width + 20);
            _activeEditor.Height = Math.Max(30, desiredSize.Height + 20);
        }

        private Size MeasureText(string text, TextBox textBox)
        {
            var ft = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBox.FontFamily, textBox.FontStyle,
                             textBox.FontWeight, textBox.FontStretch),
                textBox.FontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(textBox).PixelsPerDip
            );

            return new Size(ft.Width, ft.Height);
        }

        private void Editor_KeyDown(object sender, KeyEventArgs e)
        {
            if (_activeEditor == null)
                return;

            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                EditorToTextElement();
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CancelEditor();
            }
        }

        private void Editor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // Podle potřeby:
            // 1) automaticky potvrdit
            // CommitEditor();

            // nebo 2) nechat být
        }

        private void EditorToTextElement()
        {
            if (_activeEditor == null)
            {
                return;
            }

            string text = _activeEditor.Text;

            double x = Canvas.GetLeft(_activeEditor);
            double y = Canvas.GetTop(_activeEditor);

            CancelEditor();

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            Context.Controller.NewTextEditingFinished(x, y, text);

            //MindMap.Presentation.Components.TextElement te = MindMap.Presentation.Components.TextElement.CreateTextElement(this, x, y, text);

            //MyCanvas.Children.Add(te);
        }

        private void CancelEditor()
        {
            if (_activeEditor == null)
                return;

            MyCanvas.Children.Remove(_activeEditor);
            _activeEditor.KeyDown -= Editor_KeyDown;
            _activeEditor.TextChanged -= Editor_TextChanged;
            _activeEditor.LostKeyboardFocus -= Editor_LostKeyboardFocus;
            _activeEditor = null;
        }
    }
}
