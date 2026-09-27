using System;
using Ovomium.Features.Updater;

namespace Ovomium.Tests
{
    /// <summary>Notes de release vers rich text : entrées titre + détail, lignes libres, troncature.</summary>
    internal static class ChangelogFormatterTests
    {
        private const string Detail = "<indent=1em><size=85%><color=#BEBEBE>";

        private static int s_failures;

        public static int Run()
        {
            Check("vide", null, "");
            Check("entrée", "- **Titre** : une phrase (Feature).",
                "<b><color=orange>Titre</color></b>\n" + Detail
                + "une phrase <color=#8C8C8C>(Feature)</color></color></size></indent>");
            Check("deux entrées séparées par un demi-interligne", "- **A** : x (F).\n- **B** : y (G, H).",
                "<b><color=orange>A</color></b>\n" + Detail + "x <color=#8C8C8C>(F)</color></color></size></indent>"
                + "<size=40%>\n</size>"
                + "<b><color=orange>B</color></b>\n" + Detail + "y <color=#8C8C8C>(G, H)</color></color></size></indent>");
            Check("entrée sans feature", "- **A** : fin (voir `cfg`) ici",
                "<b><color=orange>A</color></b>\n" + Detail + "fin (voir cfg) ici</color></size></indent>");
            Check("titre de version retiré, ligne libre et puce simple", "## 1.0\n\nTexte **gras**.\n- puce",
                "Texte <b>gras</b>.\n• puce");
            string many = "";
            for (int i = 0; i < ChangelogFormatter.MaxItems + 3; i++)
                many += $"ligne {i}\n";
            string text = ChangelogFormatter.ToRichText(many);
            Expect("troncature", text.EndsWith($"ligne {ChangelogFormatter.MaxItems - 2}\n…"), text);
            Console.WriteLine(s_failures == 0 ? "ChangelogFormatter : tous les tests passent" : $"ChangelogFormatter : {s_failures} échec(s)");
            return s_failures;
        }

        private static void Check(string label, string markdown, string expected)
        {
            string actual = ChangelogFormatter.ToRichText(markdown);
            Expect(label, actual == expected, $"attendu {Show(expected)}, obtenu {Show(actual)}");
        }

        private static void Expect(string label, bool ok, string detail)
        {
            if (ok)
                return;
            s_failures++;
            Console.WriteLine($"ÉCHEC ChangelogFormatter {label} : {detail}");
        }

        private static string Show(string text) => "«" + text.Replace("\n", "⏎") + "»";
    }
}
