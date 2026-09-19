using HarmonyLib;
using Ovomium.Features.CraftFromChests;
using UnityEngine;

namespace Ovomium.Features.ItemFlight
{
    /// <summary>
    /// Départ des vols. Craft et amélioration : dès <c>InventoryGui.OnCraftPressed</c> (barre de craft démarrée, soit
    /// <c>m_craftTimer</c> à 0), sur une prévision des retraits ; la consommation réelle en fin de barre les confirme
    /// (<c>CraftFromChestsConsumePatch.Consumed</c>), et <c>m_craftTimer</c> repassé à -1 sans consommation (Annuler,
    /// panneau fermé, station quittée, inventaire plein) les annule. Marteau : pose puis consommation dans la même
    /// frame, vols sur les retraits effectifs vers le fantôme de construction relevé dans <c>Player.PlacePiece</c>.
    /// </summary>
    internal static class ItemFlightPatch
    {
        private static bool s_craftPending;
        private static Vector3 s_placeTarget;
        private static int s_placeFrame = -1;

        public static void Install()
        {
            CraftFromChestsConsumePatch.Consumed -= OnConsumed;
            CraftFromChestsConsumePatch.Consumed += OnConsumed;
        }

        public static void Unload()
        {
            CraftFromChestsConsumePatch.Consumed -= OnConsumed;
            ItemFlight.Unload();
        }

        private static bool Active(Player player) =>
            ItemFlightConfig.Enabled.Value && CraftFromChestsPatch.Active(player);

        private static void OnConsumed(Player player, System.Collections.Generic.List<Pull> taken)
        {
            if (s_craftPending)
            {
                s_craftPending = false;
                ItemFlight.CommitCraft();
                return;
            }
            if (!Active(player) || taken.Count == 0) return;
            Vector3 destination = Time.frameCount == s_placeFrame ? s_placeTarget : player.GetCenterPoint();
            ItemFlight.Launch(taken, destination, false);
        }

        [HarmonyPatch(typeof(InventoryGui), "OnCraftPressed", new System.Type[0])]
        private static class CraftPressedPatch
        {
            private static void Postfix(InventoryGui __instance)
            {
                Player player = Player.m_localPlayer;
                if (!Active(player) || __instance.m_craftTimer != 0f) return;
                Recipe recipe = __instance.m_craftRecipe;
                ItemDrop.ItemData upgrade = __instance.m_craftUpgradeItem;
                int quality = upgrade == null ? 1 : upgrade.m_quality + 1;
                int multiplier = __instance.m_multiCrafting ? __instance.m_multiCraftAmount : 1;
                recipe.GetAmount(quality, out _, out ItemDrop.ItemData single, multiplier);
                if (single != null) return;  // « un seul ingrédient au choix » : consommé hors ConsumeResources
                var pulls = PullPlan.Predict(player, recipe.m_resources, quality, -1, multiplier);
                if (pulls.Count == 0) return;
                CraftingStation station = player.GetCurrentCraftingStation();
                Vector3 destination = station != null ? ItemFlight.Center(station.gameObject) : player.GetCenterPoint();
                ItemFlight.Launch(pulls, destination, true);
                s_craftPending = true;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "Update", new System.Type[0])]
        private static class CraftWatchPatch
        {
            private static void Postfix(InventoryGui __instance)
            {
                if (!s_craftPending || __instance.m_craftTimer >= 0f) return;
                s_craftPending = false;
                ItemFlight.CancelCraft();
            }
        }

        [HarmonyPatch(typeof(Player), "PlacePiece",
            new System.Type[] { typeof(Piece), typeof(Vector3), typeof(Quaternion), typeof(bool), typeof(bool) })]
        private static class PlacePiecePatch
        {
            private static void Prefix(Player __instance, Vector3 pos)
            {
                GameObject ghost = __instance.m_placementGhost;
                s_placeTarget = ghost != null ? ItemFlight.Center(ghost) : pos + Vector3.up * 0.5f;
                s_placeFrame = Time.frameCount;
            }
        }
    }
}
