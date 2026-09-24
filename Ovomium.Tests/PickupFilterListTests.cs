using System;
using System.Collections.Generic;
using System.Linq;
using Ovomium.Features.PickupFilter;

namespace Ovomium.Tests
{
    /// <summary>Forme texte de <see cref="PickupFilterList"/> : lecture tolérante, écriture triée, aller-retour.</summary>
    internal static class PickupFilterListTests
    {
        private static int s_failures;

        public static int Run()
        {
            CheckParse("absente", null);
            CheckParse("vide", "");
            CheckParse("une entrée", "$item_stone", "$item_stone");
            CheckParse("lignes vides et espaces ignorés", "\n $item_wood \n\n$item_stone\n", "$item_stone", "$item_wood");
            CheckParse("doublon fusionné", "$item_wood\n$item_wood", "$item_wood");
            CheckSerialize("liste vide", new string[0], "");
            CheckSerialize("tri ordinal", new[] { "$item_wood", "$item_stone", "$item_Resin" },
                "$item_Resin\n$item_stone\n$item_wood");
            var names = new[] { "$item_flint", "Objet de mod", "$item_stone" };
            CheckParse("aller-retour", PickupFilterList.Serialize(names), names);
            Console.WriteLine(s_failures == 0 ? "PickupFilterList : tous les tests passent" : $"PickupFilterList : {s_failures} échec(s)");
            return s_failures;
        }

        private static void CheckParse(string label, string raw, params string[] expected)
        {
            HashSet<string> names = PickupFilterList.Parse(raw);
            if (names.SetEquals(expected))
                return;
            s_failures++;
            Console.WriteLine($"ÉCHEC {label} : attendu {{{string.Join(", ", expected)}}}, obtenu {{{string.Join(", ", names.OrderBy(n => n))}}}");
        }

        private static void CheckSerialize(string label, IEnumerable<string> names, string expected)
        {
            string raw = PickupFilterList.Serialize(names);
            if (raw == expected)
                return;
            s_failures++;
            Console.WriteLine($"ÉCHEC {label} : attendu « {expected} », obtenu « {raw} »");
        }
    }
}
