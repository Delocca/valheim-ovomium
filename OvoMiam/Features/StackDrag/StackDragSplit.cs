using HarmonyLib;
using UnityEngine;

namespace OvoMiam.Features.StackDrag
{
    /// <summary>
    /// Shift+clic avec une pile en main : le dialogue de split du jeu, borné à la quantité en main, pose N unités
    /// dans la case cliquée et laisse le reste en main. Le jeu appelle OnSplitOk soit par l'évènement SplitAccepted
    /// (bouton OK), soit directement (touche Entrée) : on intercepte donc OnSplitOk lui-même plutôt que l'évènement.
    /// </summary>
    internal static class StackDragSplit
    {
        private static InventoryGui s_gui;
        private static InventoryGrid s_grid;
        private static Vector2i s_pos;

        internal static void Open(InventoryGui gui, InventoryGrid grid, Vector2i pos)
        {
            gui.ShowSplitDialog(gui.m_dragItem, gui.m_dragInventory);
            bool altMode = ZInput.GetKey(KeyCode.LeftControl) || ZInput.GetKey(KeyCode.RightControl);
            gui.m_splitDialog.UpdateLimits(gui.m_dragAmount, altMode);
            s_gui = gui;
            s_grid = grid;
            s_pos = pos;
        }

        private static void Clear()
        {
            s_gui = null;
            s_grid = null;
        }

        /// <summary>Tout split ouvert par le jeu lui-même (mains vides) annule un split vers case en attente.</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.ShowSplitDialog),
            new System.Type[] { typeof(ItemDrop.ItemData), typeof(Inventory) })]
        private static class ShowSplitDialogPatch
        {
            private static void Prefix()
            {
                Clear();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSplitCancel), new System.Type[0])]
        private static class OnSplitCancelPatch
        {
            private static void Prefix()
            {
                Clear();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSplitOk), new System.Type[0])]
        private static class OnSplitOkPatch
        {
            private static bool Prefix(InventoryGui __instance)
            {
                if (s_gui != __instance)
                    return true;
                // Un clic sur une case pendant le dialogue a pu poser la pile (vanilla) : la main est alors vide.
                if (__instance.m_dragGo != null && __instance.m_dragItem == __instance.m_splitItem)
                {
                    int amount = (int)__instance.m_splitDialog.m_splitSlider.value;
                    StackDragPatch.MoveToCell(__instance, s_grid, s_pos, amount, emptyOnly: false);
                    // Fin de l'action : le reste retourne à la pile d'origine (il ne l'a jamais quittée).
                    __instance.SetupDragItem(null, null, 1);
                }
                __instance.m_splitItem = null;
                __instance.m_splitInventory = null;
                __instance.HideSplitDialog();
                Clear();
                return false;
            }
        }
    }
}
