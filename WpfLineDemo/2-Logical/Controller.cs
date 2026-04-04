using MindMap.Data;
using MindMap.Logical;
using MindMap.Presentation.Components;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Shapes;



//using System.Windows.Documents;
using static System.Net.Mime.MediaTypeNames;

namespace MindMap._2_Logical
{
    public class Controller
    {
        private List<(ElementBaseData, FrameworkElement)> _items = new List<(ElementBaseData, FrameworkElement)>();

        private (ElementBaseData, FrameworkElement)? _lineItem1;
        private DateTime _lineItem1ClickTime;

        public void NewTextEditingFinished(double x, double y, string text)
        {
            TextElement te = TextElement.CreateTextElement(Context.MainWindow, x, y, text);
            Context.MainWindow.MyCanvas.Children.Add(te);

            ElementBaseData elementData = new ElementBaseData()
            {
                X = x,
                Y = y,
                Text = text
            };
            Context.CurrProject.Elements.Add(elementData);

            _items.Add((elementData, te));
        }

        public int SetElementMaxZindex(FrameworkElement element)
        {
            (ElementBaseData, FrameworkElement) item = _items.First(i => i.Item2 == element);
            int index = Context.CurrProject.GetMaxZindex();
            index++; 
            item.Item1.Zindex = index;
            return index;
        }

        public void ElementStartMoving(FrameworkElement element)
        {
            (ElementBaseData, FrameworkElement) item = _items.First(i => i.Item2 == element);
            drawAllElementLines(item, false);
        }

        public void UpdateElementCoordinates(FrameworkElement element, double x, double y)
        { 
            (ElementBaseData, FrameworkElement) item = _items.First(i => i.Item2 == element);
            item.Item1.X = x;
            item.Item1.Y = y;
            drawAllElementLines(item, true);
        }

        private void drawAllElementLines((ElementBaseData, FrameworkElement) item, bool visible)
        {
            foreach (LineData line in Context.CurrProject.Lines.Where(l => l.Element1ID == item.Item1.ID || l.Element2ID == item.Item1.ID))
            {
                drawTwoElementsLine(_items.First(i => i.Item1.ID == line.Element1ID), _items.First(i => i.Item1.ID == line.Element2ID), visible);
            }
        }

        private void drawTwoElementsLine((ElementBaseData, FrameworkElement) item1, (ElementBaseData, FrameworkElement) item2, bool visible)
        {
            double x1 = item1.Item1.X + item1.Item2.ActualWidth / 2;
            double y1 = item1.Item1.Y + item1.Item2.ActualHeight / 2;
            double x2 = item2.Item1.X + item2.Item2.ActualWidth / 2;
            double y2 = item2.Item1.Y + item2.Item2.ActualHeight / 2;

            LineHelper.DrawLine(Context.MainWindow, x1, y1, x2, y2, visible);
        }

        public void LineElementSpecified(FrameworkElement element)
        {
            if (_lineItem1 != null && element != _lineItem1.Value.Item2 && DateTime.Now.Subtract(_lineItem1ClickTime).TotalSeconds < 4) // magické 4 s
            {
                var lineItem2 = _items.First(i => i.Item2 == element);

                if (Context.CurrProject.Lines.Any(l => l.Element1ID == _lineItem1.Value.Item1.ID && l.Element2ID == lineItem2.Item1.ID ||
                                                       l.Element1ID == lineItem2.Item1.ID && l.Element2ID == _lineItem1.Value.Item1.ID))
                {
                    MessageBox.Show("Taková relace už existuje.");
                    return;
                }

                LineData lineData = new LineData()
                {
                    Element1ID = _lineItem1.Value.Item1.ID,
                    Element2ID = lineItem2.Item1.ID
                };
                Context.CurrProject.Lines.Add(lineData);

                drawTwoElementsLine(_lineItem1.Value, lineItem2, true);

            }
            else
            {
                _lineItem1 = _items.First(i => i.Item2 == element);
                _lineItem1ClickTime = DateTime.Now;
            }

        }
    }
}
