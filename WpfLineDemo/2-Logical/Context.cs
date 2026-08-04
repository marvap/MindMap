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
        /// <summary>The whole file (root level of the hierarchy). Save/Open work with this.</summary>
        public static MindMapData RootProject { get; set; }

        /// <summary>The level currently shown on the canvas and edited. Equals RootProject at the root.</summary>
        public static MindMapData CurrProject { get; set; }

        public static Controller Controller { get; set; }

        public static MainWindow MainWindow { get; set; }

        public static string CurrFilePath { get; set; }
    }
}
