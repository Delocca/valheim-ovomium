using HarmonyLib;

namespace Ovomium.Features.UpgradeDiff
{
    /// <summary>
    /// Onglet Amélioration : <c>InventoryGui.UpdateRecipe</c> (chaque frame) pose dans <c>m_recipeDecription</c> le
    /// tooltip de l'objet à la qualité suivante (<c>ItemData.m_quality + 1</c>, <c>ItemData</c> = objet du joueur,
    /// null en craft). On régénère le même tooltip à la qualité actuelle et on annote chaque valeur qui change.
    /// Le résultat est mémorisé (objet, qualité, texte vanilla) : le postfix ne recalcule que si le texte change.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe", new System.Type[] { typeof(Player), typeof(float) })]
    internal static class UpgradeDiffPatch
    {
        private static ItemDrop.ItemData s_cachedItem;
        private static int s_cachedQuality;
        private static string s_cachedVanilla;
        private static string s_cachedAnnotated;

        private static void Postfix(InventoryGui __instance)
        {
            if (!UpgradeDiffConfig.Enabled.Value || !__instance.m_recipeDecription.enabled)
                return;
            Recipe recipe = __instance.m_selectedRecipe.Recipe;
            ItemDrop.ItemData item = __instance.m_selectedRecipe.ItemData;
            if (recipe == null || item == null)
                return;
            string vanilla = __instance.m_recipeDecription.text;
            if (!ReferenceEquals(item, s_cachedItem) || item.m_quality != s_cachedQuality || vanilla != s_cachedVanilla)
            {
                s_cachedItem = item;
                s_cachedQuality = item.m_quality;
                s_cachedVanilla = vanilla;
                s_cachedAnnotated = UpgradeDiffText.Annotate(CurrentTooltip(recipe, item), vanilla);
            }
            if (s_cachedAnnotated != null)
                __instance.m_recipeDecription.text = s_cachedAnnotated;
        }

        /// <summary>Même construction que le jeu (quantité, ligne « un seul ingrédient »), à la qualité actuelle.</summary>
        private static string CurrentTooltip(Recipe recipe, ItemDrop.ItemData item)
        {
            string text = Localization.instance.Localize(
                ItemDrop.ItemData.GetTooltip(recipe.m_item.m_itemData, item.m_quality, crafting: true, Game.m_worldLevel, recipe.m_amount));
            if (recipe.m_requireOnlyOneIngredient)
                text += Localization.instance.Localize("\n\n<color=orange>$inventory_onlyoneingredient</color>");
            return text;
        }
    }
}
