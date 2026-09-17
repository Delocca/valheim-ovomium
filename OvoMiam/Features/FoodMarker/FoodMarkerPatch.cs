using System.Linq;
using HarmonyLib;
using OvoMiam.Features.Food;
using TMPro;

namespace OvoMiam.Features.FoodMarker
{
    /// <summary>
    /// Après la création d'une ligne de recette, préfixe son nom (TMP_Text « name », rich text actif)
    /// d'un point coloré par stat dominante. Les lignes sont recréées à chaque rafraîchissement, donc
    /// le préfixe n'est jamais appliqué deux fois.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.AddRecipeToList),
        typeof(Player), typeof(Recipe), typeof(ItemDrop.ItemData), typeof(bool))]
    internal static class FoodMarkerPatch
    {
        private static void Postfix(InventoryGui __instance, Recipe recipe)
        {
            if (!FoodMarkerConfig.Enabled.Value)
                return;
            var profile = FoodProfile.Of(recipe.m_item.m_itemData.m_shared);
            if (!profile.IsFood)
                return;

            var element = __instance.m_availableRecipes[__instance.m_availableRecipes.Count - 1].InterfaceElement;
            var name = element.transform.Find("name").GetComponent<TMP_Text>();
            name.text = Marker(profile) + " " + name.text;
        }

        private static string Marker(FoodProfile profile)
        {
            var glyph = FoodMarkerConfig.Glyph.Value;
            var dots = string.Concat(profile.Dominant.Select(stat => $"<color={FoodColors.Hex(stat)}>{glyph}</color>"));
            return $"<size={FoodMarkerConfig.GlyphScale.Value}%>{dots}</size>";
        }
    }
}
