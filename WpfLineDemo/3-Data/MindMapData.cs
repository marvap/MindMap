using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Windows.Shapes;

namespace MindMap.Data
{
    public class MindMapData
    {
        public List<ElementBaseData> Elements { get; } = new List<ElementBaseData>();

        public List<LineData> Lines { get; } = new List<LineData>();


        public ElementBaseData GetElementData(int elementID)
        {
            return Elements.First(e => e.ID == elementID);
        }

        public int GetNextID()
        {
            return !Elements.Any() ? 1 : Elements.Max(e => e.ID) + 1;
        }

        public int GetMaxZindex()
        {
            return !Elements.Any() ? 0 : Elements.Max(e => e.Zindex);
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static MindMapData? Deserialize(string serialized)
        {
            if (string.IsNullOrEmpty(serialized))
            {
                return null;
            }

            return JsonSerializer.Deserialize<MindMapData>(serialized);
        }
    }
}
