using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Entrée des stations couvertes par <see cref="StationInput"/> (four à charbon, rouet, raffinerie d'eitr, moulin,
    /// bélier) : E ajoute une unité prise dans l'inventaire ou les coffres, comme la recharge des feux. Un objet de la
    /// barre rapide utilisé sur la station reste vanilla ; rien de disponible : vanilla (« rien à traiter »).
    /// </summary>
    [HarmonyPatch(typeof(Smelter), "OnAddOre", new System.Type[] { typeof(Switch), typeof(Humanoid), typeof(ItemDrop.ItemData) })]
    internal static class OreFromChestsPatch
    {
        private static bool Prefix(Smelter __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
        {
            Player player = user as Player;
            if (item != null || !CraftFromChestsPatch.Active(player)) return true;
            ItemDrop input = StationInput.Choose(player, __instance);
            if (!FuelFromChests.Active(player, input, __instance.m_nview)) return true;
            if (__instance.GetQueueSize() >= __instance.m_maxOre)
            {
                player.Message(MessageHud.MessageType.Center, "$msg_itsfull");
                __result = false;
                return false;
            }
            if (!FuelFromChests.Refuel(player, input, () => AddOre(__instance, input), __instance.transform,
                    "$msg_added " + input.m_itemData.m_shared.m_name)) return true;
            __result = true;
            return false;
        }

        /// <summary>Fin du <c>OnAddOre</c> vanilla : RPC (nom du prefab, non triché) puis animation d'ajout.</summary>
        private static void AddOre(Smelter smelter, ItemDrop input)
        {
            smelter.m_nview.InvokeRPC("RPC_AddOre", input.gameObject.name, false);
            smelter.m_addedOreTime = Time.time;
            if (smelter.m_addOreAnimationDuration > 0f) smelter.SetAnimation(true);
        }
    }

    /// <summary>
    /// Survol de l'entrée : « Ajouter Bois 3 (12) ». Le libellé vanilla (« Ajouter Bois », « Ajouter l'objet »,
    /// « Ajouter ») est remplacé par « Ajouter » + l'objet que E prendra, qui peut différer (bois précieux porté sur soi).
    /// </summary>
    [HarmonyPatch(typeof(Smelter), "OnHoverAddOre", new System.Type[0])]
    internal static class OreFromChestsHoverPatch
    {
        private static void Postfix(Smelter __instance, ref string __result)
        {
            Player player = Player.m_localPlayer;
            if (!CraftFromChestsPatch.Active(player)) return;
            ItemDrop input = StationInput.Choose(player, __instance);
            if (!FuelFromChests.Active(player, input, __instance.m_nview)) return;
            string tooltip = Localization.instance.Localize(__instance.m_addOreTooltip);
            if (!__result.EndsWith(tooltip)) return;
            __result = __result.Substring(0, __result.Length - tooltip.Length)
                       + Localization.instance.Localize("$piece_smelter_add " + input.m_itemData.m_shared.m_name);
            __result = FuelFromChests.Annotate(__result, player, input, true);
        }
    }
}
