using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace ProjectLink.Utils
{
    public static class CsvLoader
    {
        public static T[] Load<T>(string resourcePath) where T : new()
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogError($"[CsvLoader] Missing resource: {resourcePath}");
                return Array.Empty<T>();
            }
            return Parse<T>(asset.text);
        }

        static T[] Parse<T>(string text) where T : new()
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return Array.Empty<T>();

            var headers = ParseCsvLine(lines[0]);
            var type    = typeof(T);
            var fields  = new FieldInfo[headers.Length];
            for (int i = 0; i < headers.Length; i++)
                fields[i] = type.GetField(headers[i]);

            var results = new List<T>(lines.Length - 1);
            for (int r = 1; r < lines.Length; r++)
            {
                var values = ParseCsvLine(lines[r]);
                var obj    = new T();
                for (int c = 0; c < fields.Length; c++)
                {
                    if (fields[c] == null || c >= values.Length) continue;
                    fields[c].SetValue(obj, ConvertValue(values[c], fields[c].FieldType));
                }
                results.Add(obj);
            }
            return results.ToArray();
        }

        static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = string.Empty;
            bool inQuote = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuote && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current += '"';
                        i++;
                    }
                    else
                    {
                        inQuote = !inQuote;
                    }
                }
                else if (c == ',' && !inQuote)
                {
                    fields.Add(current.Trim());
                    current = string.Empty;
                }
                else
                {
                    current += c;
                }
            }

            fields.Add(current.Trim());
            return fields.ToArray();
        }

        static object ConvertValue(string raw, Type type)
        {
            if (string.IsNullOrEmpty(raw)) return Activator.CreateInstance(type);
            if (type == typeof(int))    return int.Parse(raw);
            if (type == typeof(uint))   return uint.Parse(raw);
            if (type == typeof(long))   return long.Parse(raw);
            if (type == typeof(ulong))  return ulong.Parse(raw);
            if (type == typeof(float))  return float.Parse(raw, CultureInfo.InvariantCulture);
            if (type == typeof(double)) return double.Parse(raw, CultureInfo.InvariantCulture);
            if (type == typeof(bool))   return bool.Parse(raw);
            return raw;
        }
    }
}
