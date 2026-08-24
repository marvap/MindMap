using MindMap.Data;
using MindMap.Logical;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using WpfLineDemo;
using static System.Net.Mime.MediaTypeNames;

namespace MindMap.Presentation.Components
{
    public class TextElement : Border
    {
        private MainWindow _ownerWindow;

        private bool _isDragging;

        // Inner visual: [ text ][ ◷ date ][ ▸ sub-level ] inside the Border. Fixed order; each shown/hidden independently.
        private readonly TextBlock _textBlock;
        private readonly TextBlock _clock;
        private readonly TextBlock _indicator;

        public string Text
        {
            get
            {
                return _textBlock.Text;
            }
            set
            {
                _textBlock.Text = value;
            }
        }

        public Point Position
        {
            get
            {
                return new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
            }
        }

        private TextElement(MainWindow owner, string text)
        {
            _ownerWindow = owner;

            MarkAsUnselected();
            CornerRadius = new CornerRadius(1);

            _textBlock = new TextBlock
            {
                Text = text,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            _clock = new TextBlock
            {
                Text = "◷", // monochrome clock-like glyph; shown only when the node has a date (tentative glyph)
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)), // dark: contrasts on all node colors + DodgerBlue
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            _indicator = new TextBlock
            {
                Text = "▸", // ▸ shown only when the node owns a sub-level
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)), // dark: contrasts on all node colors + DodgerBlue
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { _textBlock, _clock, _indicator }
            };

            MouseLeftButtonDown += Element_MouseLeftButtonDown;
            MouseMove += Element_MouseMove;
            MouseLeftButtonUp += Element_MouseLeftButtonUp;
        }

        /// <summary>
        /// Factory method
        /// </summary>
        public static TextElement CreateTextElement(MainWindow owner, double x, double y, string text, int zIndex, double fontSize)
        {
            TextElement teRet = new TextElement(owner, text);

            teRet.SetFontSize(fontSize);

            Canvas.SetLeft(teRet, x);
            Canvas.SetTop(teRet, y);

            Panel.SetZIndex(teRet, zIndex);

            // bonus: kurzor pro lepší UX
            teRet.Cursor = Cursors.SizeAll;

            return teRet;
        }

        public void SetFontSize(double fontSize)
        {
            _textBlock.FontSize = fontSize;
            _clock.FontSize = fontSize;     // keep the ◷ in scale with the node text
            _indicator.FontSize = fontSize; // keep the ▸ in scale with the node text
        }

        public void SetBold(bool bold)
        {
            _textBlock.FontWeight = bold ? FontWeights.Bold : FontWeights.Normal;
        }

        public void SetItalic(bool italic)
        {
            _textBlock.FontStyle = italic ? FontStyles.Italic : FontStyles.Normal;
        }

        /// <summary>Show/hide the ▸ indicator that marks a node owning a sub-level.</summary>
        public void SetHasChildLevel(bool hasChildLevel)
        {
            _indicator.Visibility = hasChildLevel ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>Show/hide the ◷ clock glyph that marks a node with a date set.</summary>
        public void SetHasDate(bool hasDate)
        {
            _clock.Visibility = hasDate ? Visibility.Visible : Visibility.Collapsed;
        }

        public void BringToFront()
        {
            int index = Context.Controller.SetElementMaxZindex(this);
            Panel.SetZIndex(this, index);
        }

        private NodeColorEnum _color = NodeColorEnum.Blue;
        private bool _selected;

        public void SetColor(NodeColorEnum color)
        {
            _color = color;
            applyBrushes();
        }

        public void MarkAsSelected()
        {
            _selected = true;
            applyBrushes();
            BorderThickness = new Thickness(2);
            Padding = new Thickness(4);
        }

        public void MarkAsUnselected()
        {
            _selected = false;
            applyBrushes();
            BorderThickness = new Thickness(1);
            Padding = new Thickness(5);
        }

        private void applyBrushes()
        {
            if (_selected)
            {
                Background = Brushes.DodgerBlue;
                BorderBrush = Brushes.DarkBlue;
            }
            else
            {
                (Brush background, Brush border) = colorBrushes(_color);
                Background = background;
                BorderBrush = border;
            }
        }

        private static (Brush background, Brush border) colorBrushes(NodeColorEnum color)
        {
            return color switch
            {
                NodeColorEnum.Green => (Brushes.LightGreen, Brushes.SeaGreen),
                NodeColorEnum.Red => (Brushes.LightSalmon, Brushes.IndianRed),
                NodeColorEnum.Yellow => (Brushes.Khaki, Brushes.DarkKhaki),
                _ => (Brushes.LightBlue, Brushes.SteelBlue) // Blue (default)
            };
        }

        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.L)) // Line / Link
            {
                Context.Controller.LineElementSpecified(this, LineTypeEnum.Simple);

                e.Handled = true;
                return;
            }
            else if (Keyboard.IsKeyDown(Key.O)) // Oriented line
            {
                Context.Controller.LineElementSpecified(this, LineTypeEnum.Oriented);

                e.Handled = true;
                return;
            }
            else if (Keyboard.IsKeyDown(Key.R)) // Rubber / Remove
            {
                if (e.ClickCount == 2)
                {
                    // Controller handles the confirm (and counts sub-levels if any).
                    Context.Controller.ElementDeleteRequested(this);
                }
                else
                {
                    Context.Controller.DelineElementSpecified(this);
                }

                e.Handled = true;
                return;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.ClickCount == 2)
                {
                    // Ctrl+double-click: edit the node text.
                    Context.Controller.NodeCtrlDoubleClicked(this);
                }
                else
                {
                    Context.Controller.CtrlClickSelect(this);
                }

                e.Handled = true;
                return;
            }
            else if (e.ClickCount == 2) // dvojklik
            {
                // Plain double-click: enter/create sub-level, or collapse the current selection.
                Context.Controller.NodeDoubleClicked(this);

                e.Handled = true;
                return;
            }
            else // čisté uchopení
            {
                _isDragging = true;
                Context.Controller.ElementStartMoving(this, e.GetPosition(_ownerWindow.Canvas));

                this.CaptureMouse(); // od této chvíle dostává move/up i mimo prvek
                e.Handled = true;
            }
        }

        private void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Context.Controller.ElementMoveStep(this, e.GetPosition(_ownerWindow.Canvas));
            }
        }

        private void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false; // neakceptuj další přesuny (a události)
                Context.Controller.ElementStopMoving(this, e.GetPosition(_ownerWindow.Canvas));
                
                this.ReleaseMouseCapture();

                e.Handled = true;
            }
        }

    }
}
