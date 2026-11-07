using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MultiDungeon.Localization
{
    /// <summary>
    /// 언어 테이블. Assets/Resources/Localization/strings.csv (key,ko,en, ...)를 읽는다.
    /// 언어를 추가하려면 CSV에 열을 하나 더하면 된다.
    /// </summary>
    public static class Loc
    {
        const string LanguageKey = "settings.language";
        static Dictionary<string, string[]> table;
        static string[] languages;
        static int current = -1;

        public static event Action Changed;

        public static IReadOnlyList<string> Languages { get { Load(); return languages; } }

        public static string Language
        {
            get { Load(); return languages[current]; }
        }

        public static string Get(string key)
        {
            Load();
            if (string.IsNullOrEmpty(key)) return "";
            if (!table.TryGetValue(key, out var row)) return key;
            var value = current < row.Length ? row[current] : "";
            return string.IsNullOrEmpty(value) ? row[0] : value;
        }

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);

        public static void SetLanguage(string code)
        {
            Load();
            int index = Array.IndexOf(languages, code);
            if (index < 0 || index == current) return;
            current = index;
            PlayerPrefs.SetString(LanguageKey, code);
            Changed?.Invoke();
        }

        /// <summary>다음 언어로 바꾼다 (ko → en → ko ...).</summary>
        public static void Next()
        {
            Load();
            SetLanguage(languages[(current + 1) % languages.Length]);
        }

        static void Load()
        {
            if (table != null) return;
            table = new Dictionary<string, string[]>();
            var asset = Resources.Load<TextAsset>("Localization/strings");
            var lines = asset != null ? asset.text.Replace("\r", "").Split('\n') : new string[0];
            languages = lines.Length > 0 ? Skip1(ParseLine(lines[0])) : new[] { "ko" };
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var cells = ParseLine(lines[i]);
                table[cells[0]] = Skip1(cells);
            }

            string fallback = Application.systemLanguage == SystemLanguage.Korean ? "ko" : "en";
            current = Math.Max(0, Array.IndexOf(languages, PlayerPrefs.GetString(LanguageKey, fallback)));
        }

        static string[] Skip1(List<string> cells)
        {
            var result = new string[Math.Max(0, cells.Count - 1)];
            for (int i = 1; i < cells.Count; i++) result[i - 1] = cells[i];
            return result;
        }

        /// <summary>쉼표로 나눈다. 쉼표가 든 값은 "따옴표"로 감싼다 ("" = 따옴표 한 개).</summary>
        static List<string> ParseLine(string line)
        {
            var cells = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else if (c == '"') quoted = false;
                    else sb.Append(c);
                }
                else if (c == '"') quoted = true;
                else if (c == ',') { cells.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            cells.Add(sb.ToString());
            return cells;
        }
    }
}
