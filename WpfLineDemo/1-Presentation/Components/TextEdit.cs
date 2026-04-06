using MindMap.Logical;
using MindMap.Presentation.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MindMap._1_Presentation.Components
{
    public class TextEdit : TextBox
    {
        public bool IsActive { get; set; } = true;

        private TextElement? _textElementToUpdate;


        public TextEdit(Point position, string text = "", TextElement? teToUpdate = null)
        {
            MinWidth = 120;
            MinHeight = 30;
            Text = text;
            AcceptsReturn = true; // víceřádkovost
            TextWrapping = TextWrapping.Wrap;
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            FontSize = 12;
            Padding = new Thickness(6);
            BorderThickness = new Thickness(1);
            BorderBrush = Brushes.SteelBlue;
            Background = Brushes.White;

            this.KeyDown += TextEdit_KeyDown;
            this.TextChanged += TextEdit_TextChanged;

            Canvas.SetLeft(this, position.X);
            Canvas.SetTop(this, position.Y);

            _textElementToUpdate = teToUpdate;
            teToUpdate?.Visibility = Visibility.Hidden;

            Context.MainWindow.MyCanvas.Children.Add(this);
            this.Focus();
            this.CaretIndex = 0;
        }

        private void TextEdit_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                if (_textElementToUpdate != null)
                {
                    Context.Controller.UpdateTextElementText(_textElementToUpdate, Text);
                    _textElementToUpdate.Visibility = Visibility.Visible;
                    CancelEditor();
                }
                else
                {
                    Context.MainWindow.EditorToTextElement(this);
                }
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CancelEditor();
            }
        }

        private void TextEdit_TextChanged(object sender, TextChangedEventArgs e)
        {
            ResizeEditorToContent();
        }

        private void ResizeEditorToContent()
        {
            Size desiredSize = MeasureText(Text, this);

            Width = Math.Max(120, desiredSize.Width + 20);
            Height = Math.Max(30, desiredSize.Height + 20);
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

        //private void EditorToTextElement()
        //{
        //    string text = this.Text;

        //    double x = Canvas.GetLeft(this);
        //    double y = Canvas.GetTop(this);

        //    CancelEditor();

        //    if (!string.IsNullOrWhiteSpace(text))
        //    {
        //        Context.Controller.NewTextEditingFinished(x, y, text);
        //    }
        //}

        public void CancelEditor()
        {
            Context.MainWindow.MyCanvas.Children.Remove(this);
            this.IsActive = false;
            this.KeyDown -= TextEdit_KeyDown;
            this.TextChanged -= TextEdit_TextChanged;
        }

    }
}
