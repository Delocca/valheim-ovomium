using System.Collections.Generic;
using System.Linq;
using System.Text;
using SemVer = System.Version;  // « Version » est aussi une classe du jeu (namespace global)

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Choix de la release à installer dans la réponse de l'API GitHub (liste de releases, ou release unique d'une URL
    /// <c>/latest</c> restée dans un cfg) et changelog cumulé de toutes les versions manquées. Logique pure (testée).
    /// </summary>
    internal static class ReleaseNotes
    {
        /// <summary>Au-delà, les versions plus anciennes sont résumées en une ligne.</summary>
        public const int MaxVersions = 8;
        private const string ReleasesSuffix = "/releases";

        public sealed class Pending
        {
            public SemVer Version;
            public object Release;  // la plus récente, qui porte l'archive
            public string Changelog;
        }

        /// <summary>URL de la liste des releases ; l'ancienne valeur par défaut « …/releases/latest » y est ramenée.</summary>
        public static string ListUrl(string url)
        {
            string trimmed = (url ?? "").Trim().TrimEnd('/');
            if (trimmed.EndsWith(ReleasesSuffix + "/latest"))
                trimmed = trimmed.Substring(0, trimmed.Length - "/latest".Length);
            return trimmed.EndsWith(ReleasesSuffix) ? trimmed + "?per_page=50" : trimmed;
        }

        /// <summary>Plus haute version publiée (brouillons et préversions exclus), null si aucune n'est lisible.</summary>
        public static SemVer Latest(object json) => Published(json).Select(r => r.Key).DefaultIfEmpty(null).Max();

        /// <summary>Releases plus récentes que <paramref name="current"/>, ou null si à jour.</summary>
        public static Pending Select(object json, SemVer current)
        {
            var newer = Published(json).Where(r => r.Key > current).OrderByDescending(r => r.Key).ToList();
            if (newer.Count == 0)
                return null;
            return new Pending { Version = newer[0].Key, Release = newer[0].Value, Changelog = Combine(newer) };
        }

        /// <summary>Notes seules pour une version ; sinon chacune sous un sous-titre « ## x.y.z », la plus récente d'abord.</summary>
        private static string Combine(List<KeyValuePair<SemVer, object>> newer)
        {
            if (newer.Count == 1)
                return Body(newer[0].Value);
            var sb = new StringBuilder();
            foreach (var release in newer.Take(MaxVersions))
                sb.Append($"## {release.Key}\n{Body(release.Value)}\n\n");
            int older = newer.Count - MaxVersions;
            if (older > 0)
                sb.Append(older == 1 ? "… et une version plus ancienne." : $"… et {older} versions plus anciennes.");
            return sb.ToString().TrimEnd();
        }

        private static string Body(object release) => (MiniJson.Get<string>(release, "body") ?? "").Trim();

        private static IEnumerable<KeyValuePair<SemVer, object>> Published(object json)
        {
            IEnumerable<object> releases = json as List<object>;
            if (releases == null)
                releases = json != null ? new[] { json } : new object[0];
            foreach (object release in releases)
            {
                if (Flag(release, "draft") || Flag(release, "prerelease"))
                    continue;
                string tag = MiniJson.Get<string>(release, "tag_name") ?? "";
                if (SemVer.TryParse(tag.TrimStart('v', 'V'), out SemVer version))
                    yield return new KeyValuePair<SemVer, object>(version, release);
            }
        }

        private static bool Flag(object release, string key) => MiniJson.Get<object>(release, key) is bool b && b;
    }
}
