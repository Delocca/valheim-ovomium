using HarmonyLib;
using UnityEngine;

namespace OvoMiam.Features.RecipeKeyboardNav
{
    /// <summary>
    /// À chaque frame où l'inventaire est visible, les flèches haut/bas déplacent la recette sélectionnée,
    /// comme le fait le stick de la manette (InventoryGui.UpdateRecipeGamepadInput), avec centrage dans la liste.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "Update", new System.Type[0])]
    internal static class RecipeKeyboardNavPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            if (!RecipeKeyboardNavConfig.Enabled.Value || !CanNavigate(__instance))
                return;

            int step = ZInput.GetKeyDown(KeyCode.DownArrow) ? 1 : ZInput.GetKeyDown(KeyCode.UpArrow) ? -1 : 0;
            if (step == 0)
                return;

            int last = __instance.m_availableRecipes.Count - 1;
            int index = Mathf.Clamp(__instance.GetSelectedRecipeIndex() + step, 0, last);
            __instance.SetRecipe(index, center: true);
        }

        /// <summary>Mêmes gardes que le jeu pour ses propres raccourcis d'inventaire (chat, console, menu…).</summary>
        private static bool CanNavigate(InventoryGui gui)
        {
            return InventoryGui.IsVisible()
                && gui.m_availableRecipes.Count > 0
                && (Chat.instance == null || !Chat.instance.HasFocus())
                && !Console.IsVisible()
                && !Menu.IsVisible();
        }
    }
}
