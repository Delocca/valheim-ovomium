using System.Collections.Generic;
using UnityEngine;

namespace OvoMiam.Features.StackDrag
{
    /// <summary>
    /// Double-clic sur une pile : toutes les piles du même objet de cet inventaire sont versées dans celle située
    /// le plus en bas à droite (parcours de droite à gauche puis de bas en haut), jusqu'à ce qu'elle soit pleine,
    /// et cette pile est prise en main. La pile double-cliquée ne sert qu'à désigner l'objet.
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
            ItemDrop.ItemData clicked = gui.m_dragItem;
            Inventory inventory = grid.GetInventory();
            if (gui.m_dragInventory != inventory || clicked.m_shared.m_maxStackSize <= 1)
                return false;
            List<ItemDrop.ItemData> piles = SameTypePiles(inventory, clicked);
            if (piles.Count < 2)
                return false;

            ItemDrop.ItemData receiver = piles[0];
            for (int i = 1; i < piles.Count && receiver.GetSpaceLeftInStack() > 0; i++)
            {
                int amount = Mathf.Min(piles[i].m_stack, receiver.GetSpaceLeftInStack());
                inventory.MoveItemToThis(inventory, piles[i], amount, receiver.m_gridPos.x, receiver.m_gridPos.y);
            }
            gui.SetupDragItem(receiver, inventory, receiver.m_stack);
            gui.UpdateCraftingPanel();
            return true;
        }

        /// <summary>Piles du même objet, de bas en haut puis de droite à gauche (la première est en bas à droite).</summary>
        private static List<ItemDrop.ItemData> SameTypePiles(Inventory inventory, ItemDrop.ItemData reference)
        {
            List<ItemDrop.ItemData> piles = new List<ItemDrop.ItemData>();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item.IsSameType(reference))
                    piles.Add(item);
            }
            piles.Sort((a, b) => a.m_gridPos.y != b.m_gridPos.y
                ? b.m_gridPos.y.CompareTo(a.m_gridPos.y)
                : b.m_gridPos.x.CompareTo(a.m_gridPos.x));
            return piles;
        }
    }
}
