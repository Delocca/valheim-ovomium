using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Recharge en combustible depuis les coffres : feux (torches, lampes, braséros, feux de camp, foyers : tout
    /// <c>Fireplace</c>, par <c>Interact</c>), fours, fourneau à charbon et fumoir (<c>Smelter.OnAddFuel</c>) et marmite
    /// (<c>CookingStation.OnAddFuelSwitch</c>), ces deux derniers branchés sur un <c>Switch</c>. Partout le jeu ajoute
    /// une unité de <c>m_fuelItem</c> par pression, identifiée par nom sans qualité, prise dans l'inventaire ; la
    /// branche de recharge est refaite avec inventaire + coffres, dans l'ordre de <c>ChestsFirst</c>, inversé si Ctrl
    /// est maintenu au moment de la pression (drapeau <see cref="PullOrder.Inverted"/> le temps du retrait). Le survol
    /// affiche « Résine 3 (12) » comme le panneau de craft.
    /// </summary>
    internal static class FuelFromChests
    {
        internal static bool Active(Player player, ItemDrop fuel, ZNetView nview) =>
            CraftFromChestsPatch.Active(player) && fuel != null && nview != null && nview.IsValid();

        /// <summary>Retire une unité, coffres ou inventaire d'abord selon l'ordre courant ; faux si rien n'a pu être pris.</summary>
        internal static bool TakeOne(Player player, ItemDrop item, List<Pull> taken)
        {
            string name = item.m_itemData.m_shared.m_name;
            Vector3 center = player.transform.position;
            bool chestsFirst = PullOrder.ChestsFirst;
            if (chestsFirst && NearbyChests.Remove(center, item, 1, -1, taken) == 0) return true;
            if (player.m_inventory.CountItems(name) > 0)
            {
                player.m_inventory.RemoveItem(name, 1);
                return true;
            }
            return !chestsFirst && NearbyChests.Remove(center, item, 1, -1, taken) == 0;
        }

        /// <summary>Prévision de <see cref="TakeOne"/> sans rien retirer : coffre prévu dans <paramref name="pulls"/> (vide si inventaire) ; faux si rien à prendre.</summary>
        private static bool PlanOne(Player player, ItemDrop item, List<Pull> pulls)
        {
            Vector3 center = player.transform.position;
            bool chestsFirst = PullOrder.ChestsFirst;
            if (chestsFirst && NearbyChests.Plan(center, item, 1, -1, pulls) == 0) return true;
            pulls.Clear();
            if (player.m_inventory.CountItems(item.m_itemData.m_shared.m_name) > 0) return true;
            return !chestsFirst && NearbyChests.Plan(center, item, 1, -1, pulls) == 0;
        }

        /// <summary>
        /// Prend une unité (ordre inversé si Ctrl est maintenu) puis l'ajoute au feu (message, RPC vanilla, vol vers
        /// <paramref name="target"/>) ; faux si rien à prendre. Coffre prévu pas encore à nous : rien n'est pris, message
        /// « Utilisé par quelqu'un d'autre », vrai (la pression est consommée).
        /// </summary>
        internal static bool Refuel(Player player, ItemDrop fuel, ZNetView nview, Transform target, string message)
        {
            var taken = new List<Pull>();
            PullOrder.Inverted = PullOrder.CtrlHeld;
            if (PullOrder.Inverted) PullOrder.CtrlUsed = true;
            try
            {
                var plan = new List<Pull>();
                if (!PlanOne(player, fuel, plan)) return false;
                if (!ChestReservation.EnsureOwned(player, plan, "recharge")) return true;
                if (!TakeOne(player, fuel, taken)) return false;
                player.Message(MessageHud.MessageType.Center, message);
                nview.InvokeRPC("RPC_AddFuel");
                CraftFromChestsPatch.RaiseConsumed(player, taken, HoverPoint(player, target));
                return true;
            }
            finally { PullOrder.Inverted = false; }
        }

        private static readonly RaycastHit[] s_hits = new RaycastHit[32];

        /// <summary>
        /// Point de <paramref name="target"/> visé par la caméra (même lancer de rayon que <c>Player.FindHoverObject</c>) :
        /// la bouche à charbon d'un fourneau, la flamme d'une torche… Sinon la position de l'objet.
        /// </summary>
        private static Vector3 HoverPoint(Player player, Transform target)
        {
            Transform camera = GameCamera.instance.transform;
            int count = Physics.RaycastNonAlloc(camera.position, camera.forward, s_hits, 50f, player.m_interactMask);
            RaycastHit best = default;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = s_hits[i];
                if (hit.collider.attachedRigidbody != null && hit.collider.attachedRigidbody.gameObject == player.gameObject) continue;
                if (!hit.collider.transform.IsChildOf(target)) continue;
                if (found && hit.distance >= best.distance) continue;
                best = hit;
                found = true;
            }
            return found ? best.point : target.position;
        }

        /// <summary>
        /// Complète le nom localisé du combustible dans un texte de survol : « Résine 3 (12) » puis la ligne de
        /// Ctrl + E. <paramref name="last"/> : dernière occurrence du nom (fours : le nom figure aussi dans la jauge).
        /// </summary>
        internal static string Annotate(string text, Player player, ItemDrop fuel, bool last)
        {
            string name = fuel.m_itemData.m_shared.m_name;
            string localized = Localization.instance.Localize(name);
            int at = last ? text.LastIndexOf(localized) : text.IndexOf(localized);
            if (at < 0) return text;
            int inInventory = player.m_inventory.CountItems(name);
            int total = CraftFromChestsPatch.Total(player, name, -1);
            string label = $"{localized} {inInventory} <size=70%><color=#80E080>({total})</color></size>";
            return text.Substring(0, at) + label + text.Substring(at + localized.Length) + "\n" + PullOrder.FuelHint;
        }
    }

    [HarmonyPatch(typeof(Fireplace), "Interact", new System.Type[] { typeof(Humanoid), typeof(bool), typeof(bool) })]
    internal static class FuelFromChestsFireplacePatch
    {
        /// <summary>Mêmes gardes que le vanilla, puis recharge ; le vanilla garde la main s'il n'y a rien à prendre (message « plus de … »).</summary>
        private static bool Prefix(Fireplace __instance, Humanoid user, bool hold, bool alt, ref bool __result)
        {
            Player player = user as Player;
            if (!__instance.m_canRefill || __instance.m_infiniteFuel
                || !FuelFromChests.Active(player, __instance.m_fuelItem, __instance.m_nview)) return true;
            if (hold && (__instance.m_holdRepeatInterval <= 0f
                         || Time.time - __instance.m_lastUseTime < __instance.m_holdRepeatInterval)) return true;
            if (!__instance.m_nview.HasOwner()) __instance.m_nview.ClaimOwnership();
            float fuel = __instance.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel);
            if (__instance.m_canTurnOff && !hold && !alt && fuel > 0f) return true;
            string name = __instance.m_fuelItem.m_itemData.m_shared.m_name;
            if (Mathf.CeilToInt(fuel) >= __instance.m_maxFuel)
            {
                if (CraftFromChestsPatch.Total(player, name, -1) == 0) return true;
                player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_cantaddmore", name));
                __result = false;
                return false;
            }
            if (!FuelFromChests.Refuel(player, __instance.m_fuelItem, __instance.m_nview, __instance.transform,
                    Localization.instance.Localize("$msg_fireadding", name))) return true;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Fireplace), "GetHoverText", new System.Type[0])]
    internal static class FuelFromChestsFireplaceHoverPatch
    {
        private static void Postfix(Fireplace __instance, ref string __result)
        {
            Player player = Player.m_localPlayer;
            if (__result.Length == 0 || !__instance.m_canRefill || __instance.m_infiniteFuel
                || !FuelFromChests.Active(player, __instance.m_fuelItem, __instance.m_nview)) return;
            __result = FuelFromChests.Annotate(__result, player, __instance.m_fuelItem, false);
        }
    }

    /// <summary>Fours, fourneau à charbon, fumoir : mêmes gardes vanilla (mauvais objet, plein), puis recharge ; « n'en a pas » vanilla si rien à prendre.</summary>
    [HarmonyPatch(typeof(Smelter), "OnAddFuel", new System.Type[] { typeof(Switch), typeof(Humanoid), typeof(ItemDrop.ItemData) })]
    internal static class FuelFromChestsSmelterPatch
    {
        private static bool Prefix(Smelter __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
        {
            Player player = user as Player;
            if (!FuelFromChests.Active(player, __instance.m_fuelItem, __instance.m_nview)) return true;
            string name = __instance.m_fuelItem.m_itemData.m_shared.m_name;
            if (item != null && item.m_shared.m_name != name) return true;
            if (__instance.GetFuel() > __instance.m_maxFuel - 1) return true;
            if (!FuelFromChests.Refuel(player, __instance.m_fuelItem, __instance.m_nview, __instance.transform,
                    "$msg_added " + name)) return true;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Smelter), "OnHoverAddFuel", new System.Type[0])]
    internal static class FuelFromChestsSmelterHoverPatch
    {
        private static void Postfix(Smelter __instance, ref string __result)
        {
            Player player = Player.m_localPlayer;
            if (!FuelFromChests.Active(player, __instance.m_fuelItem, __instance.m_nview)) return;
            __result = FuelFromChests.Annotate(__result, player, __instance.m_fuelItem, true);
        }
    }

    /// <summary>Marmite : corps identique à <c>Smelter.OnAddFuel</c>.</summary>
    [HarmonyPatch(typeof(CookingStation), "OnAddFuelSwitch", new System.Type[] { typeof(Switch), typeof(Humanoid), typeof(ItemDrop.ItemData) })]
    internal static class FuelFromChestsCookingPatch
    {
        private static bool Prefix(CookingStation __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
        {
            Player player = user as Player;
            if (!FuelFromChests.Active(player, __instance.m_fuelItem, __instance.m_nview)) return true;
            string name = __instance.m_fuelItem.m_itemData.m_shared.m_name;
            if (item != null && item.m_shared.m_name != name) return true;
            if (__instance.GetFuel() > __instance.m_maxFuel - 1) return true;
            if (!FuelFromChests.Refuel(player, __instance.m_fuelItem, __instance.m_nview, __instance.transform,
                    "$msg_added " + name)) return true;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(CookingStation), "OnHoverFuelSwitch", new System.Type[0])]
    internal static class FuelFromChestsCookingHoverPatch
    {
        private static void Postfix(CookingStation __instance, ref string __result)
        {
            Player player = Player.m_localPlayer;
            if (!FuelFromChests.Active(player, __instance.m_fuelItem, __instance.m_nview)) return;
            __result = FuelFromChests.Annotate(__result, player, __instance.m_fuelItem, true);
        }
    }
}
