using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Ovomium.Features.UpgradeDiff
{
    /// <summary>
    /// Annotation pure d'un tooltip d'amélioration : pour chaque ligne dont le premier nombre orange diffère de
    /// celui de la même ligne du tooltip actuel, insère la différence en vert (+) ou rouge (−) après ce nombre.
    /// Les deux textes viennent de la même fonction (<c>GetTooltip</c>), seule la qualité change : mêmes lignes,
    /// même ordre ; sinon on ne touche à rien.
    /// </summary>
    internal static class UpgradeDiffText
    {
        private static readonly Regex s_orangeNumber = new Regex(
            @"<color=orange>([+-]?[0-9]+(?:[.,][0-9]+)?)[^<]*</color>", RegexOptions.Compiled);
        private static readonly char[] s_newline = { '\n' };

        /// <summary>Texte annoté, ou <c>null</c> si les textes ne sont pas comparables ou si rien ne change.</summary>
        public static string Annotate(string current, string upgraded)
        {
            string[] currentLines = current.Split(s_newline);
            string[] upgradedLines = upgraded.Split(s_newline);
            if (currentLines.Length != upgradedLines.Length)
                return null;
            var result = new StringBuilder(upgraded.Length + 64);
            bool changed = false;
            for (int i = 0; i < upgradedLines.Length; i++)
            {
                if (i > 0)
                    result.Append('\n');
                string line = AnnotateLine(currentLines[i], upgradedLines[i]);
                changed |= line != null;
                result.Append(line ?? upgradedLines[i]);
            }
            return changed ? result.ToString() : null;
        }

        /// <summary>Ligne annotée, ou <c>null</c> si elle n'a pas de nombre orange qui change.</summary>
        private static string AnnotateLine(string current, string upgraded)
        {
            Match before = s_orangeNumber.Match(current);
            Match after = s_orangeNumber.Match(upgraded);
            if (!before.Success || !after.Success)
                return null;
            string oldText = before.Groups[1].Value;
            string newText = after.Groups[1].Value;
            if (!TryParse(oldText, out double oldValue) || !TryParse(newText, out double newValue))
                return null;
            double delta = newValue - oldValue;
            if (delta == 0)
                return null;
            bool integers = !HasSeparator(oldText) && !HasSeparator(newText);
            string magnitude = integers
                ? System.Math.Abs(delta).ToString("0", CultureInfo.InvariantCulture)
                : System.Math.Abs(delta).ToString("0.0", CultureInfo.InvariantCulture);
            string tag = delta > 0
                ? "<color=green>(+" + magnitude + ")</color>"
                : "<color=red>(-" + magnitude + ")</color>";
            int insertAt = after.Index + after.Length;
            return upgraded.Substring(0, insertAt) + " " + tag + upgraded.Substring(insertAt);
        }

        private static bool TryParse(string text, out double value) =>
            double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private static bool HasSeparator(string text) => text.IndexOf('.') >= 0 || text.IndexOf(',') >= 0;
    }
}
