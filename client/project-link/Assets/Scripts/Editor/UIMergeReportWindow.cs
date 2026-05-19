using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectLink.EditorTools
{
    public class UIMergeReportWindow : EditorWindow
    {
        UIMergeReport _report;
        Vector2 _scroll;

        public static void Show(UIMergeReport report)
        {
            var win = GetWindow<UIMergeReportWindow>("UI Merge Report");
            win._report = report;
            win._scroll = Vector2.zero;
            win.minSize = new Vector2(700, 300);
            win.Show();
        }

        void OnGUI()
        {
            if (_report == null) { EditorGUILayout.LabelField("No report."); return; }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawSection("Updated  (tool changed — applied)",    _report.updated,   new Color(0.5f, 0.8f, 1f));
            DrawSection("Skipped  — user edit preserved",        _report.skipped,   new Color(0.5f, 1f,  0.6f));
            DrawSection("Conflicts (both sides changed)",         _report.conflicts, new Color(1f,  0.6f, 0.4f));

            EditorGUILayout.EndScrollView();
        }

        static void DrawSection(string title, List<UIMergeReport.Entry> entries, Color color)
        {
            if (entries.Count == 0) return;
            var prev = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField($"── {title} ({entries.Count}) ──", EditorStyles.boldLabel);
            GUI.color = prev;
            foreach (var e in entries)
                EditorGUILayout.LabelField(
                    $"  {e.path}  [{e.comp}.{e.key}]  " +
                    $"base={e.baseVal}  user={e.userVal}  tool={e.toolVal}");
            EditorGUILayout.Space(4);
        }
    }
}
