using HarmonyLib;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Ctrl + clic sur le bouton Fabriquer (craft et amélioration : même bouton) : ordre coffres/inventaire inversé
    /// (<see cref="PullOrder.Inverted"/>) jusqu'à la fin de la barre de craft : la consommation (<c>DoCrafting</c>) n'a
    /// lieu qu'à ce moment, et ItemFlight prévoit ses vols dès le départ. Le clic passe par <c>Button.onClick</c> →
    /// <c>InventoryGui.OnCraftPressed</c> (privée), dont le Prefix lit Ctrl au moment du clic. Le survol du bouton
    /// rappelle ce que fait Ctrl + clic quand le vanilla n'a rien à y dire.
    /// </summary>
    internal static class CtrlCraftPatch
    {
        /// <summary>Craft lancé Ctrl maintenu : drapeau tenu jusqu'à ce que la barre de craft s'arrête.</summary>
        private static bool s_armed;

        [HarmonyPatch(typeof(InventoryGui), "OnCraftPressed", new System.Type[0])]
        private static class CraftPressedPatch
        {
            private static void Prefix()
            {
                s_armed = CraftFromChestsPatch.Active(Player.m_localPlayer) && PullOrder.CtrlHeld;
                PullOrder.Inverted = s_armed;
                if (s_armed) PullOrder.CtrlUsed = true;
            }
        }

        /// <summary>Barre de craft arrêtée (craft fait, Annuler, panneau fermé, départ refusé) : ordre normal.</summary>
        [HarmonyPatch(typeof(InventoryGui), "Update", new System.Type[0])]
        private static class CraftWatchPatch
        {
            private static void Postfix(InventoryGui __instance)
            {
                if (!s_armed || __instance.m_craftTimer >= 0f) return;
                s_armed = false;
                PullOrder.Inverted = false;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe", new System.Type[] { typeof(Player), typeof(float) })]
        private static class HintPatch
        {
            private static void Postfix(InventoryGui __instance, Player player)
            {
                if (!CraftFromChestsPatch.Active(player) || !__instance.m_craftButton.interactable) return;
                UITooltip tooltip = __instance.m_craftButton.GetComponent<UITooltip>();
                if (tooltip != null && tooltip.m_text.Length == 0) tooltip.m_text = PullOrder.CraftHint;
            }
        }
    }
}
