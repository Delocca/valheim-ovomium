using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.QuickStash
{
    /// <summary>
    /// Inventaire ouvert, Ctrl + clic sur un objet sans coffre ouvert ni pile en main : rangement dans le coffre proche
    /// qui convient (effet vanilla joué, vanilla sauté) ; sinon le jeu garde la main. Le drag reste « jeter ».
    /// </summary>
    internal static class QuickStashPatch
    {
        private static void PlayEffect(InventoryGui gui) =>
            gui.m_moveItemEffects.Create(gui.transform.position, Quaternion.identity);

        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem",
            new System.Type[] { typeof(InventoryGrid), typeof(ItemDrop.ItemData), typeof(Vector2i), typeof(InventoryGrid.Modifier) })]
        private static class OnSelectedItemPatch
        {
            private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, InventoryGrid.Modifier mod)
            {
                if (mod != InventoryGrid.Modifier.Move || !QuickStash.Active()) return true;
                if (__instance.m_currentContainer != null || __instance.m_dragGo != null) return true;
                if (item == null || item.m_shared.m_questItem) return true;
                if (!QuickStash.TryStash(__instance, grid.GetInventory(), item, item.m_stack)) return true;
                PlayEffect(__instance);
                return false;
            }
        }
    }
}
