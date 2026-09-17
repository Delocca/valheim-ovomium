using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.StackDrag
{
    /// <summary>
    /// Manipulation fine des piles à la souris. Alt+clic prend 1 unité (et 1 de plus à chaque Alt+clic sur le même objet).
    /// Pile en main : Ctrl+clic en pose 1 (le reste reste en main), Shift+clic ouvre le dialogue de split vers la case
    /// cliquée (<see cref="StackDragSplit"/>), et glisser bouton maintenu pose 1 unité sur chaque case vide survolée.
    /// Le clic simple pose toute la pile comme dans le jeu, mais au relâchement : c'est ce qui permet de commencer
    /// un glisser depuis la case voulue avec une pile déjà en main. Double-clic : <see cref="StackDragGather"/>.
    /// </summary>
    internal static class StackDragPatch
    {
        /// <summary>Dernière case visitée (clic ou glisser) : on n'y pose pas une seconde fois pendant le même glisser.</summary>
        private static InventoryGrid s_lastGrid;
        private static Vector2i s_lastPos;

        /// <summary>Clic simple avec une pile en main, en attente : glisser (étaler) ou relâcher (dépôt vanilla).</summary>
        private static InventoryGrid s_pressGrid;
        private static Vector2i s_pressPos;
        private static bool s_spreading;
        private static bool s_forwarding;

        internal static bool IsActive(InventoryGui gui)
        {
            return StackDragConfig.Enabled.Value && !ZInput.IsTouchActive() && !gui.m_splitDialog.IsActive;
        }

        private static bool IsAltHeld()
        {
            return ZInput.GetKey(KeyCode.LeftAlt) || ZInput.GetKey(KeyCode.RightAlt);
        }

        /// <summary>Objets de quête et équipés : on laisse le jeu gérer (déséquipement, échange de piles…).</summary>
        private static bool CanSpreadDrag(InventoryGui gui)
        {
            return gui.m_dragGo != null
                && !gui.m_dragItem.m_shared.m_questItem
                && !Player.m_localPlayer.IsItemEquiped(gui.m_dragItem);
        }

        private static void Remember(InventoryGrid grid, Vector2i pos)
        {
            s_lastGrid = grid;
            s_lastPos = pos;
        }

        private static bool IsLast(InventoryGrid grid, Vector2i pos)
        {
            return s_lastGrid == grid && s_lastPos.x == pos.x && s_lastPos.y == pos.y;
        }

        private static void Reset()
        {
            s_lastGrid = null;
            s_pressGrid = null;
            s_spreading = false;
        }

        /// <summary>
        /// Pose jusqu'à <paramref name="amount"/> unités de la pile en main dans la case, sans jamais échanger les piles
        /// (contrairement à InventoryGrid.DropItem). Retourne le nombre d'unités effectivement posées.
        /// </summary>
        internal static int MoveToCell(InventoryGui gui, InventoryGrid grid, Vector2i pos, int amount, bool emptyOnly)
        {
            ItemDrop.ItemData item = gui.m_dragItem;
            if (!gui.m_dragInventory.ContainsItem(item))
            {
                gui.SetupDragItem(null, null, 1);
                return 0;
            }
            Inventory target = grid.GetInventory();
            ItemDrop.ItemData itemAt = target.GetItemAt(pos.x, pos.y);
            if (itemAt == item || (emptyOnly && itemAt != null))
                return 0;

            // AddItem peut poser une partie seulement (pile cible presque pleine) et renvoyer false : on compte la différence.
            int before = item.m_stack;
            target.MoveItemToThis(gui.m_dragInventory, item, amount, pos.x, pos.y);
            int moved = before - item.m_stack;
            if (moved == 0)
                return 0;

            gui.m_moveItemEffects.Create(gui.transform.position, Quaternion.identity);
            gui.m_dragAmount = Mathf.Min(gui.m_dragAmount - moved, item.m_stack);
            if (gui.m_dragAmount <= 0)
                gui.SetupDragItem(null, null, 1);
            gui.UpdateCraftingPanel();
            return moved;
        }

        /// <summary>
        /// Alt+clic sur une pile du même objet que celle en main : 1 unité de plus en main. Si c'est une autre pile,
        /// l'unité est d'abord transférée vers la pile d'origine du drag (la main ne référence qu'une seule pile).
        /// </summary>
        private static void TakeOneMore(InventoryGui gui, InventoryGrid grid, ItemDrop.ItemData item)
        {
            ItemDrop.ItemData drag = gui.m_dragItem;
            if (item != drag)
            {
                if (drag.GetSpaceLeftInStack() <= 0)
                    return;
                gui.m_dragInventory.MoveItemToThis(grid.GetInventory(), item, 1, drag.m_gridPos.x, drag.m_gridPos.y);
            }
            if (gui.m_dragAmount >= drag.m_stack)
                return;
            gui.m_dragAmount++;
            gui.m_moveItemEffects.Create(gui.transform.position, Quaternion.identity);
        }

        /// <summary>Case survolée par la souris dans la grille joueur, sinon dans celle du coffre s'il est ouvert.</summary>
        private static InventoryElement GetHovered(InventoryGui gui, out InventoryGrid grid)
        {
            grid = gui.m_playerGrid;
            InventoryElement element = grid.GetHoveredElement();
            if (element == null && gui.m_containerGrid.gameObject.activeInHierarchy)
            {
                grid = gui.m_containerGrid;
                element = grid.GetHoveredElement();
            }
            return element;
        }

        /// <summary>Clic simple relâché sans glisser : dépôt de toute la pile par le jeu, sur la case du clic.</summary>
        private static void ForwardPendingClick(InventoryGui gui)
        {
            InventoryGrid grid = s_pressGrid;
            Vector2i pos = s_pressPos;
            s_pressGrid = null;
            if (gui.m_dragGo == null)
                return;
            s_forwarding = true;
            try
            {
                gui.OnSelectedItem(grid, grid.GetInventory().GetItemAt(pos.x, pos.y), pos, InventoryGrid.Modifier.Select);
            }
            finally
            {
                s_forwarding = false;
            }
        }

        /// <summary>Clic sur une case : prise de 1 (Alt), dépôt de 1 (Ctrl), split vers la case (Shift), ou clic différé.</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem),
            new System.Type[] { typeof(InventoryGrid), typeof(ItemDrop.ItemData), typeof(Vector2i), typeof(InventoryGrid.Modifier) })]
        private static class OnSelectedItemPatch
        {
            private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item,
                Vector2i pos, InventoryGrid.Modifier mod)
            {
                if (s_forwarding || !IsActive(__instance) || Player.m_localPlayer == null || Player.m_localPlayer.IsTeleporting())
                    return true;
                bool alt = IsAltHeld();
                if (__instance.m_dragGo == null)
                {
                    if (!alt || item == null || item.m_stack <= 1)
                        return true;
                    __instance.SetupDragItem(item, grid.GetInventory(), 1);
                    return false;
                }
                if (!CanSpreadDrag(__instance))
                    return true;
                if (alt && item != null && item.IsSameType(__instance.m_dragItem))
                    TakeOneMore(__instance, grid, item);
                else if (alt || mod == InventoryGrid.Modifier.Move)
                    MoveToCell(__instance, grid, pos, 1, emptyOnly: false);
                else if (mod == InventoryGrid.Modifier.Split)
                    StackDragSplit.Open(__instance, grid, pos);
                else if (!StackDragGather.TryGather(__instance, grid, pos))
                {
                    s_pressGrid = grid;
                    s_pressPos = pos;
                }
                return false;
            }

            /// <summary>La case du clic (prise ou dépôt) ne doit pas être arrosée par le glisser qui suit.</summary>
            private static void Postfix(InventoryGrid grid, Vector2i pos)
            {
                Remember(grid, pos);
                StackDragGather.NoteClick(grid, pos);
            }
        }

        /// <summary>Glisser bouton gauche maintenu avec une pile en main : 1 unité sur chaque nouvelle case vide survolée.</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateItemDrag), new System.Type[0])]
        private static class UpdateItemDragPatch
        {
            private static void Postfix(InventoryGui __instance)
            {
                if (!IsActive(__instance) || !ZInput.GetMouseButton(0) || !CanSpreadDrag(__instance))
                {
                    if (s_pressGrid != null && !s_spreading && IsActive(__instance))
                        ForwardPendingClick(__instance);
                    Reset();
                    return;
                }
                InventoryElement element = GetHovered(__instance, out InventoryGrid grid);
                if (element == null || IsLast(grid, element.Position))
                    return;
                Remember(grid, element.Position);
                // Frame du clic : c'est OnSelectedItem qui traite la case (l'ordre avec l'EventSystem n'est pas garanti).
                if (ZInput.GetMouseButtonDown(0))
                    return;
                if (s_pressGrid != null && !s_spreading)
                {
                    s_spreading = true;
                    MoveToCell(__instance, s_pressGrid, s_pressPos, 1, emptyOnly: true);
                }
                MoveToCell(__instance, grid, element.Position, 1, emptyOnly: true);
            }
        }
    }
}
