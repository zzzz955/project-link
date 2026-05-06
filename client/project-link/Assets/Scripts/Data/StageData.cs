using System.Collections.Generic;
using UnityEngine;

namespace ProjectLink.Data
{
    public class StageData
    {
        public int Width;
        public int Height;
        public int TimeLimit;
        public int[,] NodeMap;
        public int[,] CellMap;
        public Dictionary<int, Color> NodeColors;
    }
}
