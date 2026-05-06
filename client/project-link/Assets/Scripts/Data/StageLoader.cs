using System;
using System.Collections.Generic;
using ProjectLink.Data.Generated;
using ProjectLink.Utils;
using UnityEngine;

namespace ProjectLink.Data
{
    public static class StageLoader
    {
        static IngameStage[]      _stages;
        static IngameNodeColors[] _nodeColors;

        public static StageData Load(int stageId)
        {
            EnsureLoaded();
            var stage = Array.Find(_stages, s => s.stageId == stageId);
            if (stage == null) { Debug.LogError($"[StageLoader] Stage {stageId} not found"); return null; }

            var nodeMap = DecodeMap(stage.nodeMap, stage.width, stage.height);
            var cellMap = DecodeMap(stage.cellMap, stage.width, stage.height);

            // Validate even node count per group
            var groupCount = new Dictionary<int, int>();
            for (int x = 0; x < stage.width; x++)
                for (int y = 0; y < stage.height; y++)
                {
                    int g = nodeMap[x, y];
                    if (g > 0) groupCount[g] = (groupCount.TryGetValue(g, out int v) ? v : 0) + 1;
                }
            foreach (var kv in groupCount)
                if (kv.Value % 2 != 0)
                    Debug.LogWarning($"[StageLoader] Stage {stageId}: group {kv.Key} has odd node count {kv.Value}");

            var nodeColors = new Dictionary<int, Color>();
            foreach (var row in _nodeColors)
                if (ColorUtility.TryParseHtmlString(row.hexColor, out Color c))
                    nodeColors[row.nodeGroupId] = c;

            return new StageData
            {
                Width = stage.width, Height = stage.height, TimeLimit = stage.timeLimit,
                NodeMap = nodeMap, CellMap = cellMap, NodeColors = nodeColors,
            };
        }

        static int[,] DecodeMap(string map, int width, int height)
        {
            var result = new int[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    result[x, y] = FromBase36(map, (y * width + x) * 2, 2);
            return result;
        }

        static int FromBase36(string s, int start, int len)
        {
            int value = 0;
            for (int i = start; i < start + len; i++)
            {
                char c = char.ToLowerInvariant(s[i]);
                int digit = c >= 'a' ? c - 'a' + 10 : c - '0';
                value = value * 36 + digit;
            }
            return value;
        }

        static void EnsureLoaded()
        {
            if (_stages != null) return;
            _stages     = CsvLoader.Load<IngameStage>(IngameStage.ResourcePath);
            _nodeColors = CsvLoader.Load<IngameNodeColors>(IngameNodeColors.ResourcePath);
        }
    }
}
