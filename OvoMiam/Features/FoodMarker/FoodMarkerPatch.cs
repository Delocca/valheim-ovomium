using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using OvoMiam.Food;
using TMPro;

namespace OvoMiam.Features.FoodMarker
{
    /// <summary>
    /// Une fois la liste des recettes reconstruite, préfixe le nom de chaque plat (TMP_Text « name », rich text actif)
    /// d'un point coloré par stat dominante. Les lignes sont recréées à chaque rafraîchissement, donc
    /// le préfixe n'est jamais appliqué deux fois. Indépendant de l'ordre des postfix sur cette méthode.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeList), typeof(List<Recipe>))]
    internal static class FoodMarkerPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            if (!FoodMarkerConfig.Enabled.Value)
                return;
            foreach (var pair in __instance.m_availableRecipes)
            {
                var profile = FoodProfile.Of(pair.Recipe.m_item.m_itemData.m_shared);
                if (!profile.IsFood)
                    continue;
                var name = pair.InterfaceElement.transform.Find("name").GetComponent<TMP_Text>();
                name.text = Marker(profile) + " " + name.text;
            }
        }

        private static string Marker(FoodProfile profile)
        {
            var glyph = FoodMarkerConfig.Glyph.Value;
            var dots = string.Concat(profile.Dominant.Select(stat => $"<color={FoodColors.Hex(stat)}>{glyph}</color>"));
            return $"<size={FoodMarkerConfig.GlyphScale.Value}%>{dots}</size>";
        }
    }
}
