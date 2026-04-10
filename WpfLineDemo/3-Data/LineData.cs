using MindMap.Logical;
using System;
using System.Collections.Generic;
using System.Text;

namespace MindMap.Data
{
    public class LineData
    {
        public LineTypeEnum Type { get; set; }

        public int Element1ID { get; set; }
        
        public int Element2ID { get; set; }


        public (ElementBaseData, ElementBaseData) GetLinkedElements()
        {
            return (Context.CurrProject.Elements.First(e => e.ID == Element1ID), Context.CurrProject.Elements.First(e => e.ID == Element2ID));
        }
    }
}
