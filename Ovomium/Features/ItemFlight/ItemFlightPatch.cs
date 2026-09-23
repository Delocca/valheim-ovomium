using HarmonyLib;
using Ovomium.Features.CraftFromChests;
using UnityEngine;

namespace Ovomium.Features.ItemFlight
{
    /// <summary>
    /// Départ des vols. Craft et amélioration : dès <c>InventoryGui.OnCraftPressed</c> (barre de craft démarrée, soit
    /// <c>m_craftTimer</c> à 0), sur une prévision des retraits ; la consommation réelle en fin de barre les confirme
    /// (<c>CraftFromChestsPatch.Consumed</c>), et <c>m_craftTimer</c> repassé à -1 sans consommation (Annuler,
    /// panneau fermé, station quittée, inventaire plein) les annule. Marteau : pose puis consommation dans la même
    /// frame, vols sur les retraits effectifs vers le fantôme de construction relevé dans <c>Player.PlacePiece</c>.
    /// Feux : destination fournie par l'événement. Les retraits effectifs sont aussi diffusés aux autres joueuses
    /// (<see cref="FlightBroadcast"/>) : chez elles, le vol d'un craft part à la consommation, pas au clic.
    /// </summary>
    internal static class ItemFlightPatch
    {
        private static bool s_craftPending;
        private static Vector3 s_placeTarget;
        private static int s_placeFrame = -1;

        public static void Install()
        {
            CraftFromChestsPatch.Consumed -= OnConsumed;
            CraftFromChestsPatch.Consumed += OnConsumed;
            FlightBroadcast.Install();
        }

        public static void Unload()
        {
            CraftFromChestsPatch.Consumed -= OnConsumed;
            FlightBroadcast.Unload();
            ItemFlight.Unload();
        }

        private static bool Active(Player player) =>
            ItemFlightConfig.Enabled.Value && CraftFromChestsPatch.Active(player);

        /// <summary>
        /// Retraits effectués : diffusés aux autres joueuses (même si nos vols sont désactivés, le choix est chez elles),
        /// puis vol local, sauf pour un craft dont les vols sont déjà partis au clic (ils deviennent non annulables).
        /// </summary>
        private static void OnConsumed(Player player, System.Collections.Generic.List<Pull> taken, Vector3? destination)
        {
            bool craft = !destination.HasValue && Time.frameCount != s_placeFrame;
            Vector3 target = destination ?? (craft ? CraftTarget(player) : s_placeTarget);
            if (taken.Count > 0) FlightBroadcast.SendToPoint(taken, target);
            if (craft && s_craftPending)
            {
                s_craftPending = false;
                ItemFlight.CommitCraft();
                return;
            }
            if (Active(player) && taken.Count > 0) ItemFlight.Launch(taken, target, false);
        }

        /// <summary>Centre de la station de craft, sinon de la joueuse (craft à la main).</summary>
        private static Vector3 CraftTarget(Player player)
        {
            CraftingStation station = player.GetCurrentCraftingStation();
            return station != null ? ItemFlight.Center(station.gameObject) : player.GetCenterPoint();
        }

        [HarmonyPatch(typeof(InventoryGui), "OnCraftPressed", new System.Type[0])]
        private static class CraftPressedPatch
        {
            private static void Postfix(InventoryGui __instance)
            {
                Player player = Player.m_localPlayer;
                if (!Active(player) || __instance.m_craftTimer != 0f) return;
                var pulls = PullPlan.PredictCraft(__instance, player);
                if (pulls == null || pulls.Count == 0) return;
                ItemFlight.Launch(pulls, CraftTarget(player), true);
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
