using System;
using ProjectLink.Data.Generated;
using ProjectLink.Utils;
using UnityEngine;

namespace ProjectLink.Data
{
    public static class StageLoader
    {
        static IngameStageInfo[]  _infos;
        static IngameStageNodes[] _nodes;

        public static StageData Load(int stageId)
        {
            EnsureLoaded();

            var info = Array.Find(_infos, i => i.stageId == stageId);
            if (info == null)
            {
                Debug.LogError($"[StageLoader] Stage {stageId} not found");
                return null;
            }

            return new StageData
            {
                Info  = info,
                Nodes = Array.FindAll(_nodes, n => n.stageId == stageId),
            };
        }

        static void EnsureLoaded()
        {
            if (_infos != null) return;
            _infos = CsvLoader.Load<IngameStageInfo>(IngameStageInfo.ResourcePath);
            _nodes = CsvLoader.Load<IngameStageNodes>(IngameStageNodes.ResourcePath);
        }
    }
}
