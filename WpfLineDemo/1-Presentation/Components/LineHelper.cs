using MindMap.Data;
using MindMap.Logical;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using WpfLineDemo;

namespace MindMap.Presentation.Components
{
    public class LineHelper
    {
        /// <summary>
        /// Factory method
        /// </summary>
        public static void DrawLine(MainWindow ownerWindow, double x1, double y1, double x2, double y2)
        {
            drawLine(ownerWindow, x1, y1, x2, y2, true);
        }

        /// <summary>
        /// Factory method
        /// </summary>
        public static void RemoveLine(MainWindow ownerWindow, double x1, double y1, double x2, double y2)
        {
            drawLine(ownerWindow, x1, y1, x2, y2, false);
        }

        private static void drawLine(MainWindow ownerWindow, double x1, double y1, double x2, double y2, bool visible)
        {
            Line line = new Line()
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2
            };

            line.Stroke = visible ? Brushes.Black : Brushes.White;
            line.StrokeThickness = 1;

            ownerWindow.Canvas.Children.Add(line);
        }
    }
}
