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


        public TextEdit(Point position, string? text = "", TextElement? teToUpdate = null)
        {
            MinWidth = 120;
            MinHeight = 30;
            Text = text ?? "";
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

            Context.MainWindow.MyCanvas.Children.Add(this);
            this.Focus();
            this.CaretIndex = Text.Length;
        }

        private void TextEdit_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Context.Controller.EditorToTextElement(this);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Text = string.Empty;
                Context.Controller.EditorToTextElement(this);
                e.Handled = true;
            }
        }

        private void TextEdit_TextChanged(object sender, TextChangedEventArgs e)
        {
            ResizeEditorToContent();
        }

        private void ResizeEditorToContent()
        {
            string textToMeasure = Text;
            if (textToMeasure.EndsWith("\n"))
            {
                textToMeasure = textToMeasure + "x"; // force him to consider new line as full-fledged content
            }

            Size desiredSize = MeasureText(textToMeasure);

            Width = Math.Max(120, desiredSize.Width + 20);
            Height = Math.Max(30, desiredSize.Height + 20);
        }

        private Size MeasureText(string text)
        {
            var ft = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(this.FontFamily, this.FontStyle,
                             this.FontWeight, this.FontStretch),
                this.FontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );

            return new Size(ft.Width, ft.Height);
        }

        public void CancelEditor()
        {
            Context.MainWindow.MyCanvas.Children.Remove(this);
            this.IsActive = false;
            this.KeyDown -= TextEdit_KeyDown;
            this.TextChanged -= TextEdit_TextChanged;
        }

    }
}
