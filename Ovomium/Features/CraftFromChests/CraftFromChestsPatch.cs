using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Comptage des ingrédients : <c>Player.HaveRequirementItems</c> (recettes) et <c>HaveRequirements(Piece, mode)</c>
    /// (marteau) lisent le champ <c>m_inventory</c> en direct ; quand le vanilla dit non, on refait son calcul avec
    /// inventaire + coffres. Consommation : <c>Player.ConsumeResources</c>, point unique du craft, de l'amélioration et
    /// de la pose de pièce, remplacé (coffres puis inventaire, ou l'inverse selon <c>ChestsFirst</c>). Affichage : <c>InventoryGui.SetupRequirement</c>
    /// (statique, partagée par le panneau de craft et le HUD du marteau). Aucun effet de scène ici ; l'état de réservation est dans <c>ChestReservation.Unload()</c>.
    /// Non couvert : la branche « un seul ingrédient au choix » de <c>DoCrafting</c> quand l'inventaire n'en porte aucun.
    /// </summary>
    internal static class CraftFromChestsPatch
    {
        internal static bool Active(Player player) =>
            CraftFromChestsConfig.Enabled.Value && player != null && player == Player.m_localPlayer;

        /// <summary>Même filtre d'entrée que le vanilla (ressources d'améliorateur selon la station courante).</summary>
        internal static bool Applies(Piece.Requirement req, CraftingStation station)
        {
            if (!req.m_resItem) return false;
            if (station != null) return station.m_upgrader == req.m_upgraderResource;
            return !req.m_upgraderResource;
        }

        internal static int Total(Player player, string name, int quality) =>
            player.m_inventory.CountItems(name, quality) + NearbyChests.Count(player.transform.position, name, quality);
    }

    [HarmonyPatch(typeof(Player), "HaveRequirementItems",
        new System.Type[] { typeof(Recipe), typeof(bool), typeof(int), typeof(int) })]
    internal static class CraftFromChestsRecipePatch
    {
        private static void Postfix(Player __instance, Recipe piece, bool discover, int qualityLevel, int amount, ref bool __result)
        {
            if (__result || discover || !CraftFromChestsPatch.Active(__instance)) return;
            __result = HaveItems(__instance, piece, qualityLevel, amount);
        }

        /// <summary>Boucle vanilla, avec les coffres : par niveau de qualité, le meilleur niveau doit suffire.</summary>
        private static bool HaveItems(Player player, Recipe recipe, int qualityLevel, int amount)
        {
            CraftingStation station = player.GetCurrentCraftingStation();
            foreach (Piece.Requirement req in recipe.m_resources)
            {
                if (!CraftFromChestsPatch.Applies(req, station)) continue;
                int need = req.GetAmount(qualityLevel) * amount;
                int best = 0;
                string name = req.m_resItem.m_itemData.m_shared.m_name;
                for (int q = 1; q < req.m_resItem.m_itemData.m_shared.m_maxQuality + 1; q++)
                    best = Mathf.Max(best, CraftFromChestsPatch.Total(player, name, q));
                if (recipe.m_requireOnlyOneIngredient) { if (best >= need) return true; }
                else if (best < need) return false;
            }
            return !recipe.m_requireOnlyOneIngredient;
        }
    }

    [HarmonyPatch(typeof(Player), "HaveRequirements", new System.Type[] { typeof(Piece), typeof(Player.RequirementMode) })]
    internal static class CraftFromChestsPiecePatch
    {
        private static void Postfix(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
        {
            if (__result || mode == Player.RequirementMode.IsKnown || !CraftFromChestsPatch.Active(__instance)) return;
            __result = Preamble(__instance, piece, mode) && HaveResources(__instance, piece, mode);
        }

        /// <summary>Préambule vanilla : station, DLC, clé de construction gratuite.</summary>
        private static bool Preamble(Player player, Piece piece, Player.RequirementMode mode)
        {
            if ((bool)piece.m_craftingStation)
            {
                if (mode == Player.RequirementMode.CanAlmostBuild)
                {
                    if (!player.m_knownStations.ContainsKey(piece.m_craftingStation.m_name)) return false;
                }
                else if (!CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, player.transform.position)
                         && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench))
                    return false;
            }
            if (piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(piece.m_dlc)) return false;
            return true;
        }

        private static bool HaveResources(Player player, Piece piece, Player.RequirementMode mode)
        {
            if (ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey())) return true;
            foreach (Piece.Requirement req in piece.m_resources)
            {
                if (!req.m_resItem || req.m_amount <= 0) continue;
                int total = CraftFromChestsPatch.Total(player, req.m_resItem.m_itemData.m_shared.m_name, -1);
                int need = mode == Player.RequirementMode.CanAlmostBuild ? 1 : req.m_amount;
                if (total < need) return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), "ConsumeResources",
        new System.Type[] { typeof(Piece.Requirement[]), typeof(int), typeof(int), typeof(int) })]
    internal static class CraftFromChestsConsumePatch
    {
        private static bool Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel, int itemQuality, int multiplier)
        {
            if (!CraftFromChestsPatch.Active(__instance)) return true;
            CraftingStation station = __instance.GetCurrentCraftingStation();
            foreach (Piece.Requirement req in requirements)
            {
                if (!CraftFromChestsPatch.Applies(req, station)) continue;
                int need = req.GetAmount(qualityLevel) * multiplier;
                if (need <= 0) continue;
                string name = req.m_resItem.m_itemData.m_shared.m_name;
                int missing = CraftFromChestsConfig.ChestsFirst.Value
                    ? TakeFromInventory(__instance, name, NearbyChests.Remove(__instance.transform.position, name, need, itemQuality), itemQuality)
                    : NearbyChests.Remove(__instance.transform.position, name, TakeFromInventory(__instance, name, need, itemQuality), itemQuality);
                if (missing > 0)
                    Plugin.Log.LogWarning($"CraftFromChests : {missing} × {Localization.instance.Localize(name)} introuvable(s) à la consommation");
            }
            return false;
        }

        /// <summary>Retire de l'inventaire ce qu'il contient, jusqu'à <paramref name="amount"/> ; retourne le reliquat.</summary>
        private static int TakeFromInventory(Player player, string name, int amount, int itemQuality)
        {
            int taken = Mathf.Min(player.m_inventory.CountItems(name, itemQuality), amount);
            if (taken > 0) player.m_inventory.RemoveItem(name, taken, itemQuality);
            return amount - taken;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "SetupRequirement", new System.Type[]
        { typeof(Transform), typeof(Piece.Requirement), typeof(Player), typeof(bool), typeof(int), typeof(int) })]
    internal static class CraftFromChestsDisplayPatch
    {
        private static void Postfix(Transform elementRoot, Piece.Requirement req, Player player, int quality, int craftMultiplier, bool __result)
        {
            if (!__result || req.m_resItem == null || !CraftFromChestsPatch.Active(player)) return;
            int need = req.GetAmount(quality) * craftMultiplier;
            int total = CraftFromChestsPatch.Total(player, req.m_resItem.m_itemData.m_shared.m_name, -1);
            TMP_Text amount = elementRoot.Find("res_amount").GetComponent<TMP_Text>();
            amount.richText = true;
            amount.textWrappingMode = TextWrappingModes.NoWrap;
            amount.overflowMode = TextOverflowModes.Overflow;
            amount.text = $"{need} <size=70%><color=#80E080>({total})</color></size>";
            if (total >= need) amount.color = Color.white;
            RequirementBackdrop.Fit(amount);
        }
    }
}
