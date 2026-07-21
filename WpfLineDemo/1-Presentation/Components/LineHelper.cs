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
        public static void DrawLine(MainWindow ownerWindow, double x1, double y1, double x2, double y2, bool visible, bool oriented = false)
        {
            drawLine(ownerWindow, x1, y1, x2, y2, visible);

            if (oriented)
            {
                drawArrowHead(ownerWindow, x1, y1, x2, y2, visible);
            }
        }

        /// <summary>
        /// Nakreslí hrot šipky uprostřed spojnice, směřující od [x1,y1] k [x2,y2].
        /// Hrot je tvořen dvěma úsečkami ("V"), aby se dal mazat stejným způsobem
        /// jako samotná spojnice (přemalováním na bílo).
        /// </summary>
        private static void drawArrowHead(MainWindow ownerWindow, double x1, double y1, double x2, double y2, bool visible)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6)
            {
                return; // uzly na sobě - hrot nemá kam mířit
            }

            double ux = dx / len;
            double uy = dy / len;

            // špička hrotu je uprostřed spojnice
            double mx = (x1 + x2) / 2;
            double my = (y1 + y2) / 2;

            const double headLength = 12;
            const double angle = 25 * Math.PI / 180; // poloviční úhel hrotu

            // směr "dozadu" od špičky (proti směru spojnice)
            double rx = -ux;
            double ry = -uy;

            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);

            // dvě ramínka hrotu = vektor r pootočený o ±angle
            double b1x = mx + headLength * (rx * cos - ry * sin);
            double b1y = my + headLength * (rx * sin + ry * cos);
            double b2x = mx + headLength * (rx * cos + ry * sin);
            double b2y = my + headLength * (-rx * sin + ry * cos);

            drawLine(ownerWindow, mx, my, b1x, b1y, visible);
            drawLine(ownerWindow, mx, my, b2x, b2y, visible);
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
            line.StrokeThickness = visible ? 1 : 3;

            ownerWindow.Canvas.Children.Add(line);
        }
    }
}
