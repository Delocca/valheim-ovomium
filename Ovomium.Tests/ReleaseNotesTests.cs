using System;
using Ovomium.Features.Updater;

namespace Ovomium.Tests
{
    /// <summary>Réponse de l'API GitHub vers release à installer et changelog cumulé.</summary>
    internal static class ReleaseNotesTests
    {
        private static int s_failures;

        public static int Run()
        {
            const string api = "https://api.github.com/repos/o/r/releases";
            Expect("URL /latest ramenée à la liste", ReleaseNotes.ListUrl(api + "/latest") == api + "?per_page=50");
            Expect("URL de liste", ReleaseNotes.ListUrl(api + "/") == api + "?per_page=50");
            Expect("URL autre gardée", ReleaseNotes.ListUrl(" file:///tmp/x.json ") == "file:///tmp/x.json");

            string list = "[" + Release("v1.5.0", "cinq", prerelease: true) + "," + Release("v1.4.1", "quatre-un")
                + "," + Release("v1.4.0", "quatre") + "," + Release("1.3.0", "trois") + "," + Release("brouillon", "x")
                + "," + Release("v1.6.0", "six", draft: true) + "]";
            object json = MiniJson.Parse(list);
            var pending = ReleaseNotes.Select(json, new Version(1, 3, 0));
            Expect("plus récente publiée", pending?.Version == new Version(1, 4, 1));
            Expect("cumul, plus récente d'abord", pending?.Changelog == "## 1.4.1\nquatre-un\n\n## 1.4.0\nquatre",
                pending?.Changelog);
            pending = ReleaseNotes.Select(json, new Version(1, 4, 0));
            Expect("une seule version : pas de sous-titre", pending?.Changelog == "quatre-un", pending?.Changelog);
            Expect("à jour", ReleaseNotes.Select(json, new Version(1, 4, 1)) == null);
            Expect("dernière publiée", ReleaseNotes.Latest(json) == new Version(1, 4, 1));
            Expect("objet unique (/latest)",
                ReleaseNotes.Select(MiniJson.Parse(Release("v2.0", "deux")), new Version(1, 0))?.Changelog == "deux");

            string many = "[";
            for (int i = 1; i <= ReleaseNotes.MaxVersions + 2; i++)
                many += (i > 1 ? "," : "") + Release($"v1.{i}", $"n{i}");
            pending = ReleaseNotes.Select(MiniJson.Parse(many + "]"), new Version(1, 0));
            Expect("versions anciennes résumées", pending?.Changelog.EndsWith("… et 2 versions plus anciennes.") == true
                && !pending.Changelog.Contains("n2\n"), pending?.Changelog);

            Console.WriteLine(s_failures == 0 ? "ReleaseNotes : tous les tests passent" : $"ReleaseNotes : {s_failures} échec(s)");
            return s_failures;
        }

        private static string Release(string tag, string body, bool draft = false, bool prerelease = false)
        {
            return $"{{\"tag_name\":\"{tag}\",\"body\":\"{body}\",\"draft\":{Bool(draft)},\"prerelease\":{Bool(prerelease)}}}";
        }

        private static string Bool(bool value) => value ? "true" : "false";

        private static void Expect(string label, bool ok, string detail = "")
        {
            if (ok)
                return;
            s_failures++;
            Console.WriteLine($"ÉCHEC ReleaseNotes {label} : «{detail?.Replace("\n", "⏎")}»");
        }
    }
}
