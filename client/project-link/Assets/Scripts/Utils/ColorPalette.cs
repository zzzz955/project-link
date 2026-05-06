using System.Collections.Generic;
using UnityEngine;

namespace ProjectLink.Utils
{
    public static class ColorPalette
    {
        static Dictionary<int, Color> _colors = new();

        public static void Init(Dictionary<int, Color> nodeColors)
        {
            _colors = nodeColors ?? new Dictionary<int, Color>();
        }

        public static Color Get(int colorId)
        {
            return _colors.TryGetValue(colorId, out var c) ? c : Color.magenta;
        }
    }
}
