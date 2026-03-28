using MindMap._2_Logical;
using MindMap.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using WpfLineDemo;

namespace MindMap.Logical
{
    public static class Context
    {
        public static MindMapData CurrProject { get; set; }

        public static Controller Controller { get; set; }

        public static MainWindow MainWindow { get; set; }
    }
}
