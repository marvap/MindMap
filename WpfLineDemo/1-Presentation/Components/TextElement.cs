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

        public string Text
        {
            get 
            {
                return (Child as TextBlock)?.Text;
            }
            set
            {
                (Child as TextBlock)?.Text = value;
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
            Child = new TextBlock
            {
                Text = text,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Left
            };

            MouseLeftButtonDown += Element_MouseLeftButtonDown;
            MouseMove += Element_MouseMove;
            MouseLeftButtonUp += Element_MouseLeftButtonUp;
        }

        /// <summary>
        /// Factory method
        /// </summary>
        public static TextElement CreateTextElement(MainWindow owner, double x, double y, string text, int zIndex)
        {
            TextElement teRet = new TextElement(owner, text);

            Canvas.SetLeft(teRet, x);
            Canvas.SetTop(teRet, y);

            Panel.SetZIndex(teRet, zIndex);

            // bonus: kurzor pro lepší UX
            teRet.Cursor = Cursors.SizeAll;

            return teRet;
        }

        public void BringToFront()
        {
            int index = Context.Controller.SetElementMaxZindex(this);
            Panel.SetZIndex(this, index);
        }

        public void MarkAsSelected()
        {
            Background = Brushes.DodgerBlue;
            BorderBrush = Brushes.DarkBlue;
            BorderThickness = new Thickness(2);
            Padding = new Thickness(4);
        }

        public void MarkAsUnselected()
        {
            Background = Brushes.LightBlue;
            BorderBrush = Brushes.SteelBlue;
            BorderThickness = new Thickness(1);
            Padding = new Thickness(5);
        }

        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.L)) // Line / Link
            {
                Context.Controller.LineElementSpecified(this);

                e.Handled = true;
                return;
            }
            else if (Keyboard.IsKeyDown(Key.R)) // Rubber / Remove
            {
                if (e.ClickCount == 2)
                {
                    var result = MessageBox.Show(
                        Context.MainWindow,
                        "Opravdu chcete smazat tento element?",
                        "Potvrzení",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Context.Controller.ElementRemovalRequested(this);
                    }
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
                Context.Controller.SetElementAsSelected(this);

                e.Handled = true;
                return;
            }
            else if (e.ClickCount == 2) // dvojklik
            {
                Context.Controller.TextElementEditRequested(this);

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
