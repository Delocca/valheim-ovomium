using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Clic du milieu sur le bouton Fabriquer (craft et amélioration : même bouton) : même départ que le clic gauche
    /// (<c>InventoryGui.OnCraftPressed</c>, privée, branchée sur <c>Button.onClick</c> qui ignore ce bouton), ordre
    /// coffres/inventaire inversé (<see cref="PullOrder.Inverted"/>) jusqu'à la fin de la barre de craft : la
    /// consommation (<c>DoCrafting</c>) n'a lieu qu'à ce moment, et ItemFlight prévoit ses vols dès le départ. Le relais
    /// est un composant posé sur le GameObject du bouton (<see cref="MiddleClickRelay"/>, retiré par <see cref="Unload"/>).
    /// Le survol du bouton rappelle ce que fait le clic du milieu quand le vanilla n'a rien à y dire.
    /// </summary>
    internal static class MiddleClickCraftPatch
    {
        /// <summary>Clic du milieu en cours de relais vers <c>OnCraftPressed</c>.</summary>
        private static bool s_pending;
        /// <summary>Craft lancé au clic du milieu : drapeau tenu jusqu'à ce que la barre de craft s'arrête.</summary>
        private static bool s_armed;

        /// <summary>Rechargement à chaud : le panneau existe déjà, son <c>Awake</c> ne rejouera pas.</summary>
        public static void Install()
        {
            if (InventoryGui.instance != null) Attach(InventoryGui.instance);
        }

        public static void Unload()
        {
            s_pending = false;
            s_armed = false;
            PullOrder.Inverted = false;
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || gui.m_craftButton == null) return;
            var relay = gui.m_craftButton.GetComponent<MiddleClickRelay>();
            if (relay != null) Object.Destroy(relay);
        }

        private static void Attach(InventoryGui gui)
        {
            if (gui.m_craftButton == null || gui.m_craftButton.GetComponent<MiddleClickRelay>() != null) return;
            gui.m_craftButton.gameObject.AddComponent<MiddleClickRelay>().OnMiddleClick = () => Press(gui);
        }

        /// <summary>Mêmes conditions que le clic gauche (bouton actif, pas de craft en cours), puis départ inversé.</summary>
        private static void Press(InventoryGui gui)
        {
            if (!CraftFromChestsPatch.Active(Player.m_localPlayer) || !gui.m_craftButton.interactable || gui.m_craftTimer >= 0f) return;
            s_pending = true;
            try { gui.OnCraftPressed(); }
            finally { s_pending = false; }
        }

        [HarmonyPatch(typeof(InventoryGui), "Awake", new System.Type[0])]
        private static class AwakePatch
        {
            private static void Postfix(InventoryGui __instance) => Attach(__instance);
        }

        [HarmonyPatch(typeof(InventoryGui), "OnCraftPressed", new System.Type[0])]
        private static class CraftPressedPatch
        {
            private static void Prefix()
            {
                s_armed = s_pending;
                PullOrder.Inverted = s_armed;
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
                if (tooltip != null && tooltip.m_text.Length == 0) tooltip.m_text = PullOrder.MiddleClickHint;
            }
        }
    }

    /// <summary>Relais du clic du milieu d'un objet d'interface (le <c>Button</c> Unity ne réagit qu'au clic gauche).</summary>
    internal sealed class MiddleClickRelay : MonoBehaviour, IPointerClickHandler
    {
        public System.Action OnMiddleClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Middle) OnMiddleClick?.Invoke();
        }
    }
}
