using System.Collections.Generic;
using UnityEngine;

namespace OvoMiam.Features.StackDrag
{
    /// <summary>
    /// Double-clic sur une pile : les autres piles du même objet de cet inventaire y sont versées (parcours depuis le
    /// bas à droite, vers la gauche puis vers le haut) jusqu'à ce qu'elle soit pleine, et elle reste en main.
    /// Au second clic, la pile double-cliquée est déjà en main (le premier clic l'a prise) : c'est m_dragItem.
    /// </summary>
    internal static class StackDragGather
    {
        private static InventoryGrid s_clickGrid;
        private static Vector2i s_clickPos;
        private static float s_clickTime = -1f;

        /// <summary>À appeler après chaque clic traité : mémorise la case pour détecter le double-clic suivant.</summary>
        internal static void NoteClick(InventoryGrid grid, Vector2i pos)
        {
            s_clickGrid = grid;
            s_clickPos = pos;
            s_clickTime = Time.unscaledTime;
        }

        private static bool IsDoubleClick(InventoryGrid grid, Vector2i pos)
        {
            return s_clickGrid == grid && s_clickPos.x == pos.x && s_clickPos.y == pos.y
                && Time.unscaledTime - s_clickTime <= StackDragConfig.DoubleClickSeconds.Value;
        }

        /// <summary>Second clic d'un double-clic sur la pile en main : rassemble. Faux si ce n'en est pas un ou si rien à faire.</summary>
        internal static bool TryGather(InventoryGui gui, InventoryGrid grid, Vector2i pos)
        {
            if (!IsDoubleClick(grid, pos))
                return false;
            ItemDrop.ItemData receiver = gui.m_dragItem;
            Inventory inventory = grid.GetInventory();
            if (gui.m_dragInventory != inventory || receiver.m_shared.m_maxStackSize <= 1
                || inventory.GetItemAt(pos.x, pos.y) != receiver)
                return false;
            List<ItemDrop.ItemData> others = OtherPiles(inventory, receiver);
            if (others.Count == 0)
                return false;

            for (int i = 0; i < others.Count && receiver.GetSpaceLeftInStack() > 0; i++)
            {
                int amount = Mathf.Min(others[i].m_stack, receiver.GetSpaceLeftInStack());
                inventory.MoveItemToThis(inventory, others[i], amount, receiver.m_gridPos.x, receiver.m_gridPos.y);
            }
            // Même pile en main, quantité mise à jour.
            gui.SetupDragItem(receiver, inventory, receiver.m_stack);
            gui.UpdateCraftingPanel();
            return true;
        }

        /// <summary>Autres piles du même objet, de bas en haut puis de droite à gauche (la première est en bas à droite).</summary>
        private static List<ItemDrop.ItemData> OtherPiles(Inventory inventory, ItemDrop.ItemData reference)
        {
            List<ItemDrop.ItemData> piles = new List<ItemDrop.ItemData>();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item != reference && item.IsSameType(reference))
                    piles.Add(item);
            }
            piles.Sort((a, b) => a.m_gridPos.y != b.m_gridPos.y
                ? b.m_gridPos.y.CompareTo(a.m_gridPos.y)
                : b.m_gridPos.x.CompareTo(a.m_gridPos.x));
            return piles;
        }
    }
}
