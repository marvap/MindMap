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
    }
}
