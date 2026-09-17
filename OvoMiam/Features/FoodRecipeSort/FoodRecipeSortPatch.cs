using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using OvoMiam.Food;
using UnityEngine;

namespace OvoMiam.Features.FoodRecipeSort
{
    /// <summary>
    /// Après que le jeu a construit et trié la liste des recettes, la re-trie quand la station courante
    /// est une station de cuisine configurée, puis repositionne les éléments d'interface
    /// (le jeu les place à la main : anchoredPosition = index * -m_recipeListSpace, pas de LayoutGroup).
    /// Sûr vis-à-vis de la sélection : le jeu mémorise la recette sélectionnée par valeur, pas par index.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeList), typeof(List<Recipe>))]
    internal static class FoodRecipeSortPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            if (!FoodRecipeSortConfig.Enabled.Value)
                return;
            var station = CurrentStation();
            if (station == null || !FoodRecipeSortConfig.Stations.Contains(station.m_name))
                return;

            var recipes = __instance.m_availableRecipes;
            var sorted = Sort(recipes);
            recipes.Clear();
            recipes.AddRange(sorted);
            Reposition(__instance);
            if (FoodRecipeSortConfig.LogSortOrder.Value)
                LogOrder(station, recipes);
        }

        private static CraftingStation CurrentStation()
        {
            return Player.m_localPlayer ? Player.m_localPlayer.GetCurrentCraftingStation() : null;
        }

        /// <summary>Tri stable : craftable, groupe de stat, score décroissant (chaque clé selon la config), l'ordre du jeu départageant.</summary>
        private static List<InventoryGui.RecipeDataPair> Sort(List<InventoryGui.RecipeDataPair> recipes)
        {
            var keyed = recipes.Select(pair => (pair, profile: ProfileOf(pair)));
            IOrderedEnumerable<(InventoryGui.RecipeDataPair pair, FoodProfile profile)> ordered =
                keyed.OrderByDescending(k => FoodRecipeSortConfig.CraftableFirst.Value && k.pair.CanCraft);
            if (FoodRecipeSortConfig.GroupByStat.Value)
                ordered = ordered.ThenBy(k => k.profile.GroupRank);
            return ordered.ThenByDescending(k => k.profile.Score).Select(k => k.pair).ToList();
        }

        private static FoodProfile ProfileOf(InventoryGui.RecipeDataPair pair)
        {
            return FoodProfile.Of(pair.Recipe.m_item.m_itemData.m_shared);
        }

        private static void Reposition(InventoryGui gui)
        {
            for (int i = 0; i < gui.m_availableRecipes.Count; i++)
            {
                var rect = gui.m_availableRecipes[i].InterfaceElement.transform as RectTransform;
                rect.anchoredPosition = new Vector2(0f, i * -gui.m_recipeListSpace);
            }
        }

        private static void LogOrder(CraftingStation station, List<InventoryGui.RecipeDataPair> recipes)
        {
            var lines = recipes.Select(pair =>
            {
                var profile = ProfileOf(pair);
                var stats = string.Join("+", profile.Dominant.Select(s => s.ToString()));
                return $"{profile.Score,6:0.#}  {(pair.CanCraft ? "ok " : "-- ")}{stats,-22}{pair.Recipe.m_item.m_itemData.m_shared.m_name}";
            });
            Plugin.Log.LogInfo($"{station.m_name} trié :\n" + string.Join("\n", lines));
        }
    }
}
