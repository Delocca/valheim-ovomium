using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Ovomium.Features.ServerWake
{
    /// <summary>
    /// Lecture pure des réponses HTTP de Nodecraft : délai <c>Retry-After</c> d'un 429 et résumé lisible d'une réponse
    /// inattendue pour le journal (titre et texte d'une page Cloudflare). Testée dans Ovomium.Tests.
    /// </summary>
    internal static class NodecraftHttp
    {
        public const int DefaultRetrySeconds = 30;
        public const int MaxRetrySeconds = 120;
        private const int SnippetLength = 200;
        private const RegexOptions Html = RegexOptions.IgnoreCase | RegexOptions.Singleline;

        /// <summary>Attente après un 429 : en-tête en secondes ou en date HTTP, 30 s à défaut, entre 1 et 120 s.</summary>
        public static int RetryAfterSeconds(string header, DateTime nowUtc)
        {
            int seconds = DefaultRetrySeconds;
            string value = (header ?? "").Trim();
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int delta))
                seconds = delta;
            else if (DateTime.TryParseExact(value, "r", CultureInfo.InvariantCulture,
                         DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime date))
                seconds = (int)Math.Ceiling((date - nowUtc).TotalSeconds);
            return Math.Max(1, Math.Min(MaxRetrySeconds, seconds));
        }

        /// <summary>Une ligne de journal : code, en-têtes utiles présents, puis titre et texte (page) ou corps (JSON).</summary>
        public static string Describe(int code, string retryAfter, string cfRay, string server, string body)
        {
            var parts = new List<string> { $"HTTP {code}" };
            AddIfAny(parts, "Retry-After", retryAfter);
            AddIfAny(parts, "cf-ray", cfRay);
            AddIfAny(parts, "server", server);
            string trimmed = (body ?? "").TrimStart();
            if (trimmed.StartsWith("<", StringComparison.Ordinal))
            {
                AddIfAny(parts, "titre", Quote(PageTitle(trimmed)));
                parts.Add($"texte {Quote(TextSnippet(trimmed, SnippetLength))}");
            }
            else
                parts.Add($"corps {Quote(Truncate(Collapse(trimmed), SnippetLength))}");
            return string.Join(", ", parts);
        }

        /// <summary>Contenu de la balise <c>&lt;title&gt;</c>, entités décodées, ou chaîne vide.</summary>
        public static string PageTitle(string html)
        {
            Match match = Regex.Match(html ?? "", @"<title[^>]*>(.*?)</title\s*>", Html);
            return match.Success ? Collapse(WebUtility.HtmlDecode(match.Groups[1].Value)) : "";
        }

        /// <summary>Texte visible d'une page (en-tête, scripts, styles et balises retirés, blancs réduits), tronqué.</summary>
        public static string TextSnippet(string html, int max)
        {
            string text = Regex.Replace(html ?? "", @"<(head|script|style)\b.*?</\1\s*>", " ", Html);
            text = Regex.Replace(text, "<[^>]*>", " ");
            return Truncate(Collapse(WebUtility.HtmlDecode(text)), max);
        }

        public static string Truncate(string text, int max)
        {
            text = text ?? "";
            return text.Length <= max ? text : text.Substring(0, max) + "…";
        }

        public static string Collapse(string text) => Regex.Replace(text ?? "", @"\s+", " ").Trim();

        private static string Quote(string text) => text.Length == 0 ? "" : $"« {text} »";

        private static void AddIfAny(List<string> parts, string label, string value)
        {
            if (!string.IsNullOrEmpty(value))
                parts.Add($"{label} {value}");
        }
    }
}
