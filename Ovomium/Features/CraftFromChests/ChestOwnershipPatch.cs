using System.Collections.Generic;
using HarmonyLib;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Coffres à nous avant toute consommation (<see cref="ChestReservation.EnsureOwned"/>), sinon l'action est annulée
    /// sans rien consommer. Craft et amélioration : coffres prévus demandés dès le clic sur Fabriquer
    /// (<c>OnCraftPressed</c>) et gardés tant que la barre tourne, vérifiés en tête de <c>InventoryGui.DoCrafting</c>
    /// (fin de barre ; son appelant remet la barre à -1, les vols prévus par ItemFlight s'annulent d'eux-mêmes). Pose :
    /// en tête de <c>Player.TryPlacePiece</c>, placement valide seulement (un refus vanilla garde son message).
    /// Recharge des feux : <c>FuelFromChests.Refuel</c>.
    /// </summary>
    internal static class ChestOwnershipPatch
    {
        /// <summary>Garde renouvelée à chaque frame de la barre de craft.</summary>
        private const float CraftHold = 0.5f;

        private static readonly List<Container> s_craftChests = new List<Container>();

        [HarmonyPatch(typeof(InventoryGui), "OnCraftPressed", new System.Type[0])]
        private static class CraftPressedPatch
        {
            private static void Postfix(InventoryGui __instance)
            {
                s_craftChests.Clear();
                Player player = Player.m_localPlayer;
                if (!CraftFromChestsPatch.Active(player) || __instance.m_craftTimer != 0f) return;
                var pulls = PullPlan.PredictCraft(__instance, player);
                if (pulls == null) return;
                foreach (var pull in pulls)
                {
                    if (s_craftChests.Contains(pull.Chest)) continue;
                    s_craftChests.Add(pull.Chest);
                    ChestReservation.RequestNow(pull.Chest);
                }
            }
        }

        /// <summary>Barre en cours : garde des coffres prévus ; arrêtée (craft fait ou annulé) : fin de la garde.</summary>
        [HarmonyPatch(typeof(InventoryGui), "Update", new System.Type[0])]
        private static class CraftHoldPatch
        {
            private static void Postfix(InventoryGui __instance)
            {
                if (s_craftChests.Count == 0) return;
                if (__instance.m_craftTimer < 0f) { s_craftChests.Clear(); return; }
                foreach (var chest in s_craftChests)
                    if (chest != null) ChestReservation.Hold(chest, CraftHold);
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "DoCrafting", new System.Type[] { typeof(Player) })]
        private static class DoCraftingPatch
        {
            private static bool Prefix(InventoryGui __instance, Player player)
            {
                if (!CraftFromChestsPatch.Active(player)) return true;
                var pulls = PullPlan.PredictCraft(__instance, player);
                return pulls == null || ChestReservation.EnsureOwned(player, pulls, "fabrication");
            }
        }

        [HarmonyPatch(typeof(Player), "TryPlacePiece", new System.Type[] { typeof(Piece) })]
        private static class PlacePatch
        {
            private static bool Prefix(Player __instance, Piece piece, ref bool __result)
            {
                if (!CraftFromChestsPatch.Active(__instance) || __instance.m_noPlacementCost
                    || __instance.m_placementStatus != Player.PlacementStatus.Valid
                    || ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey())) return true;
                var pulls = PullPlan.Predict(__instance, piece.m_resources, 0, -1, 1);
                if (ChestReservation.EnsureOwned(__instance, pulls, "pose")) return true;
                __result = false;
                return false;
            }
        }
    }
}
