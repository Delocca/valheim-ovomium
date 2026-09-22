using HarmonyLib;

namespace Ovomium.Features.ChestFill
{
    /// <summary>
    /// Tout placement automatique dans une case vide (Ctrl+clic, E maintenu, Tout empiler, loot de démolition, repli
    /// de Tout prendre…) passe par <c>Inventory.FindEmptySlot(topFirst)</c>, où <c>topFirst</c> ne dépend que du type
    /// d'objet (<c>Inventory.TopFirst</c> : armes, outils, boucliers, utilitaires… en haut, le reste en bas) : le « bas
    /// d'abord » des coffres n'est que l'effet de bord de la règle qui garde la barre rapide du joueur libre des
    /// matériaux. Coffre : on force le haut. Joueur : les objets « bas d'abord » prennent les lignes 2+ de haut en bas,
    /// puis la barre rapide, puis les cases HotbarSlots ; les autres gardent le vanilla.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), "FindEmptySlot", new System.Type[] { typeof(bool) })]
    internal static class ChestFillSlotPatch
    {
        private static bool Prefix(Inventory __instance, ref bool topFirst, ref Vector2i __result)
        {
            if (!ChestFillConfig.Enabled.Value)
                return true;
            if (!ChestFill.IsLocalPlayer(__instance))
            {
                topFirst = true;
                return true;
            }
            if (!ChestFillConfig.PlayerInventory.Value || topFirst)
                return true;
            __result = ChestFill.FindPlayerSlot(__instance);
            return false;
        }
    }

    /// <summary>
    /// La pile à compléter est, en vanilla, la première non pleine dans l'ordre d'insertion de la liste, quelle que
    /// soit sa position : on prend à la place celle la plus en haut à gauche (pour le joueur, la barre rapide en
    /// premier : une pile qu'on y garde se recharge au ramassage).
    /// </summary>
    [HarmonyPatch(typeof(Inventory), "FindFreeStackItem", new System.Type[] { typeof(string), typeof(int), typeof(float) })]
    internal static class ChestFillStackPatch
    {
        private static bool Prefix(Inventory __instance, string name, int quality, float worldLevel, ref ItemDrop.ItemData __result)
        {
            if (!ChestFillConfig.Enabled.Value)
                return true;
            if (ChestFill.IsLocalPlayer(__instance) && !ChestFillConfig.PlayerInventory.Value)
                return true;
            __result = ChestFill.FindTopLeftStack(__instance, name, quality, worldLevel);
            return false;
        }
    }

    internal static class ChestFill
    {
        public static bool IsLocalPlayer(Inventory inventory) =>
            Player.m_localPlayer != null && inventory == Player.m_localPlayer.GetInventory();

        /// <summary>Lignes 2+ de haut en bas (cases HotbarSlots exclues), puis la barre rapide, puis les cases HotbarSlots.</summary>
        public static Vector2i FindPlayerSlot(Inventory inventory)
        {
            for (int y = 1; y < inventory.m_height; y++)
                for (int x = 0; x < inventory.m_width; x++)
                {
                    var cell = new Vector2i(x, y);
                    if (!IsHotbarCell(cell) && inventory.GetItemAt(x, y) == null)
                        return cell;
                }
            for (int x = 0; x < inventory.m_width; x++)
                if (inventory.GetItemAt(x, 0) == null)
                    return new Vector2i(x, 0);
            for (int slot = 0; slot < HotbarSlots.HotbarSlots.ExtraCount; slot++)
            {
                var cell = HotbarSlots.HotbarSlots.Cell(slot);
                if (IsHotbarCell(cell) && inventory.GetItemAt(cell.x, cell.y) == null)
                    return cell;
            }
            return new Vector2i(-1, -1);
        }

        private static bool IsHotbarCell(Vector2i cell) =>
            HotbarSlots.HotbarSlots.Active && HotbarSlots.HotbarSlots.SlotOf(cell) >= 0;

        public static ItemDrop.ItemData FindTopLeftStack(Inventory inventory, string name, int quality, float worldLevel)
        {
            ItemDrop.ItemData best = null;
            foreach (var item in inventory.m_inventory)
            {
                if (item.m_shared.m_name != name || item.m_quality != quality || item.m_stack >= item.m_shared.m_maxStackSize
                    || (float)item.m_worldLevel != worldLevel)
                    continue;
                if (best == null || item.m_gridPos.y < best.m_gridPos.y
                    || (item.m_gridPos.y == best.m_gridPos.y && item.m_gridPos.x < best.m_gridPos.x))
                    best = item;
            }
            return best;
        }
    }
}
