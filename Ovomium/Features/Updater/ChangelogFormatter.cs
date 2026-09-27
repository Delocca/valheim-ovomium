using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Notes de release (markdown de CHANGELOG.md) vers le rich text TextMeshPro de la fenêtre. Une entrée
    /// <c>- **Titre** : phrase (Feature).</c> devient son titre en gras orange puis la phrase en retrait, plus petite et
    /// grise (nom de feature plus pâle), un demi-interligne entre deux entrées ; les autres lignes gardent gras et puces.
    /// Titres retirés, lignes vides compactées, tronqué à <see cref="MaxItems"/> lignes source (le corps de
    /// UnifiedPopup ne défile pas).
    /// </summary>
    internal static class ChangelogFormatter
    {
        public const int MaxItems = 12;
        private const string TitleColor = "orange";  // couleur des mises en avant du jeu dans ses propres textes
        private const string DetailColor = "#BEBEBE";
        private const string FeatureColor = "#8C8C8C";
        private const string EntryGap = "<size=40%>\n</size>";  // ligne vide réduite : le saut de ligne porte la taille
        private static readonly Regex Entry = new Regex(@"^\s*[-*]\s+\*\*(.+?)\*\*\s*:\s*(.*)$", RegexOptions.Compiled);
        private static readonly Regex Feature = new Regex(@"\s*(\([A-Za-z0-9, ]+\))\.?\s*$", RegexOptions.Compiled);
        private static readonly Regex Bold = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
        private static readonly Regex Bullet = new Regex(@"^\s*[-*]\s+", RegexOptions.Compiled);
        private static readonly Regex Heading = new Regex(@"^\s*#+\s*", RegexOptions.Compiled);
        private static readonly Regex Code = new Regex(@"`([^`]*)`", RegexOptions.Compiled);

        public static string ToRichText(string markdown)
        {
            List<string> items = Items(markdown);
            if (items.Count > MaxItems)
            {
                items.RemoveRange(MaxItems - 1, items.Count - (MaxItems - 1));
                items.Add("…");
            }
            var parts = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0 && IsEntry(items[i]) && IsEntry(items[i - 1]))
                    parts.Add(EntryGap);
                else if (i > 0)
                    parts.Add("\n");
                parts.Add(items[i]);
            }
            return string.Concat(parts);
        }

        /// <summary>Une ligne formatée par ligne source, sans titres ni lignes vides en double ou en bordure.</summary>
        private static List<string> Items(string markdown)
        {
            var items = new List<string>();
            bool lastBlank = true;
            foreach (string raw in (markdown ?? "").Replace("\r", "").Split('\n'))
            {
                string line = Format(raw);
                bool blank = line.Trim().Length == 0;
                if (blank && lastBlank)
                    continue;
                items.Add(blank ? "" : line);
                lastBlank = blank;
            }
            while (items.Count > 0 && items[items.Count - 1].Length == 0)
                items.RemoveAt(items.Count - 1);
            return items;
        }

        private static bool IsEntry(string item) => item.StartsWith("<b><color=" + TitleColor);

        private static string Format(string line)
        {
            if (Heading.IsMatch(line))
                return "";  // le titre de version est déjà l'en-tête de la fenêtre
            Match entry = Entry.Match(line);
            if (entry.Success)
                return FormatEntry(Inline(entry.Groups[1].Value), Inline(entry.Groups[2].Value));
            return Inline(Bullet.Replace(line, "• "));
        }

        private static string FormatEntry(string title, string detail)
        {
            detail = Feature.Replace(detail, $" <color={FeatureColor}>$1</color>");
            return $"<b><color={TitleColor}>{title}</color></b>\n"
                + $"<indent=1em><size=85%><color={DetailColor}>{detail}</color></size></indent>";
        }

        private static string Inline(string text)
        {
            text = Bold.Replace(text, "<b>$1</b>");
            return Code.Replace(text, "$1");
        }
    }
}
