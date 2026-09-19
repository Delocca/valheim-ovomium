using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Notes de release (markdown de CHANGELOG.md) vers le rich text TextMeshPro de la fenêtre : gras, puces, titres
    /// retirés, lignes vides compactées, tronqué à <see cref="MaxLines"/> lignes (le corps de UnifiedPopup ne défile
    /// pas).
    /// </summary>
    internal static class ChangelogFormatter
    {
        public const int MaxLines = 12;
        private static readonly Regex Bold = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
        private static readonly Regex Bullet = new Regex(@"^\s*[-*]\s+", RegexOptions.Compiled);
        private static readonly Regex Heading = new Regex(@"^\s*#+\s*", RegexOptions.Compiled);
        private static readonly Regex Code = new Regex(@"`([^`]*)`", RegexOptions.Compiled);

        public static string ToRichText(string markdown)
        {
            var lines = new List<string>();
            bool lastBlank = true;
            foreach (string raw in (markdown ?? "").Replace("\r", "").Split('\n'))
            {
                string line = Format(raw);
                bool blank = line.Trim().Length == 0;
                if (blank && lastBlank)
                    continue;
                lines.Add(blank ? "" : line);
                lastBlank = blank;
            }
            while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
                lines.RemoveAt(lines.Count - 1);
            if (lines.Count > MaxLines)
            {
                lines.RemoveRange(MaxLines - 1, lines.Count - (MaxLines - 1));
                lines.Add("…");
            }
            return lines.Count == 0 ? "" : string.Join("\n", lines);
        }

        private static string Format(string line)
        {
            if (Heading.IsMatch(line))
                return "";  // le titre de version est déjà l'en-tête de la fenêtre
            string text = Bullet.Replace(line, "• ");
            text = Bold.Replace(text, "<b>$1</b>");
            return Code.Replace(text, "$1");
        }
    }
}
