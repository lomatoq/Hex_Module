using System;
using System.Collections.Generic;
using System.Text;

namespace HexWords.EditorTools
{
    public static class CsvUtility
    {
        public static List<string[]> Parse(string content)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrWhiteSpace(content))
            {
                return rows;
            }

            var row = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < content.Length; i++)
            {
                var c = content[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (!inQuotes && c == ',')
                {
                    row.Add(field.ToString().Trim());
                    field.Clear();
                    continue;
                }

                if (!inQuotes && (c == '\n' || c == '\r'))
                {
                    if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                    {
                        i++;
                    }

                    row.Add(field.ToString().Trim());
                    field.Clear();

                    if (row.Count > 1 || !string.IsNullOrWhiteSpace(row[0]))
                    {
                        rows.Add(row.ToArray());
                    }

                    row = new List<string>();
                    continue;
                }

                field.Append(c);
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString().Trim());
                rows.Add(row.ToArray());
            }

            return rows;
        }

        public static Dictionary<string, int> HeaderIndex(string[] headers)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                map[headers[i]] = i;
            }

            return map;
        }

        public static string Get(string[] row, Dictionary<string, int> idx, string key)
        {
            if (!idx.TryGetValue(key, out var col) || col < 0 || col >= row.Length)
            {
                return string.Empty;
            }

            return row[col];
        }
    }
}
