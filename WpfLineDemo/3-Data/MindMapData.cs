using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Shapes;

namespace MindMap.Data
{
    public class MindMapData
    {
        public List<ElementBaseData> Elements { get; set; } = new List<ElementBaseData>();

        public List<LineData> Lines { get; set; } = new List<LineData>();

        public Size WindowSize { get; set; }

        public WindowState WindowState { get; set; }

        //public ElementBaseData GetElementData(int elementID)
        //{
        //    return Elements.First(e => e.ID == elementID);
        //}

        public int GetNextID()
        {
            return !Elements.Any() ? 1 : Elements.Max(e => e.ID) + 1;
        }

        public int GetNextMaxZindex()
        {
            return (!Elements.Any() ? 0 : Elements.Max(e => e.Zindex)) + 1;
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

            MindMapData mmd = JsonSerializer.Deserialize<MindMapData>(serialized);
            mmd.Normalize();
            return mmd;
        }

        public void Normalize()
        {
            // Normalize IDs
            //
            List<int> ids = new List<int>();
            foreach (ElementBaseData element in Elements)
            {
                ids.Add(element.ID);
            }
            ids.Sort((a, b) => a.CompareTo(b));
            
            List<(int, int)> idsMap = new List<(int, int)> ();
            for(int i = 0; i < ids.Count; i++)
            {
                idsMap.Add((ids[i], i + 1));
            }

            foreach (ElementBaseData element in Elements)
            {
                var item = idsMap.First(i => i.Item1 == element.ID);
                element.ID = item.Item2;
            }
            foreach (LineData line in Lines)
            {
                var item = idsMap.First(i => i.Item1 == line.Element1ID);
                line.Element1ID = item.Item2;
                item = idsMap.First(i => i.Item1 == line.Element2ID);
                line.Element2ID = item.Item2;
            }

            // Normalize Z-indexes
            //
            List<int> zindexes = new List<int>();
            foreach (ElementBaseData element in Elements)
            {
                zindexes.Add(element.Zindex);
            }
            ids.Sort((a, b) => a.CompareTo(b));

            List<(int, int)> zindexesMap = new List<(int, int)>();
            for (int i = 0; i < zindexes.Count; i++)
            {
                zindexesMap.Add((zindexes[i], i + 1));
            }

            foreach (ElementBaseData element in Elements)
            {
                var item = zindexesMap.First(i => i.Item1 == element.Zindex);
                element.Zindex = item.Item2;
            }
        }

        public void ShiftIDsByN(int n)
        {
            foreach (ElementBaseData element in Elements)
            {
                element.ID += n;
            }
            foreach (LineData line in Lines)
            {
                line.Element1ID += n;
                line.Element2ID += n;
            }
        }

        public void ShiftZindexesByN(int n)
        {
            foreach (ElementBaseData element in Elements)
            {
                element.Zindex += n;
            }
        }

        public double GetMinX()
        {
            return !Elements.Any() ? 0 : Elements.Min(e => e.X);
        }
        public double GetMinY()
        {
            return !Elements.Any() ? 0 : Elements.Min(e => e.Y);
        }
        public void ShiftXsByZ(double z)
        {
            foreach (ElementBaseData element in Elements)
            {
                element.X += z;
            }
        }
        public void ShiftYsByZ(double z)
        {
            foreach (ElementBaseData element in Elements)
            {
                element.Y += z;
            }
        }
    }
}
