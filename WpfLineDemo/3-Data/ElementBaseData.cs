using MindMap.Logical;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Windows;

namespace MindMap.Data
{
    public class ElementBaseData
    {
        public ElementBaseData()
        { 
            ID = Context.CurrProject.GetNextID();
        }

        public int ID { get; set; }

        public ElementTypeEnum Type { get; set; }

        public double X { get; set; }
        public double Y { get; set; }

        public string Text { get; set; }

        public int Zindex { get; set; }

        public double FontSize { get; set; } = 12;

        public bool Bold { get; set; }

        public bool Italic { get; set; }

        public NodeColorEnum Color { get; set; }

        /// <summary>
        /// Optional nested sub-map owned by this node (hierarchy). Null for a plain node.
        /// Additive + nullable on purpose: old flat .mmd files (without this field)
        /// deserialize to null and keep working unchanged.
        /// </summary>
        public MindMapData? ChildLevel { get; set; }


        public ElementBaseData Clone()
        {
            return new ElementBaseData()
            {
                ID = this.ID,
                Type = this.Type,
                X = this.X,
                Y = this.Y,
                Text = this.Text,
                Zindex = this.Zindex,
                FontSize = this.FontSize,
                Bold = this.Bold,
                Italic = this.Italic,
                Color = this.Color,
                ChildLevel = this.ChildLevel?.DeepClone() // deep-clone the whole subtree
            };
        }
    }
}
