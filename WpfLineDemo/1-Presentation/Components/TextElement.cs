using MindMap.Data;
using MindMap.Logical;
using System;
using System.Collections.Generic;
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
        //private ElementBaseData _elementData;

        private MainWindow _ownerWindow;

        //private static FrameworkElement? _LineElement1;
        //private static DateTime _LineElementClickTime;

        // Text element
        private static FrameworkElement? _dragElement;
        private static bool _isDragging;
        private static Point _mouseStart;      // pozice myši vůči plátnu při začátku drag
        private static Point _elementStart;    // původní Left/Top prvku při začátku drag


        private TextElement(MainWindow owner, string text)
        {
            _ownerWindow = owner;

           // _elementData = null;

            Background = Brushes.LightBlue;
            BorderBrush = Brushes.SteelBlue;
            BorderThickness = new Thickness(2);
            CornerRadius = new CornerRadius(2);
            Padding = new Thickness(8);
            Child = new TextBlock
            {
                Text = text,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Left
            };

            // důležité: aby bral myš i když klikneš na text uvnitř
            MouseLeftButtonDown += Element_MouseLeftButtonDown;
            MouseMove += Element_MouseMove;
            MouseLeftButtonUp += Element_MouseLeftButtonUp;
        }

        /// <summary>
        /// Factory method
        /// </summary>
        public static TextElement CreateTextElement(MainWindow owner, double x, double y, string text)
        {
            TextElement teRet = new TextElement(owner, text);

            Canvas.SetLeft(teRet, x);
            Canvas.SetTop(teRet, y);

            // bonus: kurzor pro lepší UX
            teRet.Cursor = Cursors.SizeAll;

            return teRet;
        }


        void BringToFront(FrameworkElement element)
        {
            int index = Context.Controller.SetElementMaxZindex(this);
            Panel.SetZIndex(this, index);
        }


        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragElement = (FrameworkElement)sender;

            BringToFront(_dragElement);
            
            _isDragging = true;

            _mouseStart = e.GetPosition(_ownerWindow.Canvas);

            double left = Canvas.GetLeft(_dragElement);
            double top = Canvas.GetTop(_dragElement);

            // když Left/Top nebylo nastaveno, vrací NaN → ošetříme
            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;

            _elementStart = new Point(left, top);

            _dragElement.CaptureMouse(); // od této chvíle dostává move/up i mimo prvek
            e.Handled = true;
        }

        private void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _dragElement is null) return;

            Point mouseNow = e.GetPosition(_ownerWindow.Canvas);
            Vector delta = mouseNow - _mouseStart;

            Canvas.SetLeft(_dragElement, _elementStart.X + delta.X);
            Canvas.SetTop(_dragElement, _elementStart.Y + delta.Y);
        }

        private void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_ownerWindow.KeyPressed == Key.L)
            {
                Context.Controller.LineElementSpecified(this);

                e.Handled = true;
                return;
            }

            if (_dragElement is null) return;

            Context.Controller.UpdateElementCoordinates(_dragElement, Canvas.GetLeft(_dragElement), Canvas.GetTop(_dragElement));

            _isDragging = false;
            _dragElement.ReleaseMouseCapture();
            _dragElement = null;

            e.Handled = true;
        }

    }
}
