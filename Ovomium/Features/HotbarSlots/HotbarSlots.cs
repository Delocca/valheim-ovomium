using BepInEx.Configuration;
using UnityEngine;

namespace Ovomium.Features.HotbarSlots
{
    /// <summary>
    /// Deux emplacements rapides de plus sans toucher à l'inventaire : les cases 9 et 10 de la barre sont deux cases
    /// choisies de la grille (hors 1re ligne). Pour la barre, leur position est projetée en (8,0) et (9,0) le temps
    /// d'<c>UpdateIcons</c>, qui dessine alors 10 cases comme si la 1re ligne en avait 10.
    /// </summary>
    internal static class HotbarSlots
    {
        public const int VanillaCount = 8;
        public const int ExtraCount = 2;

        public static bool Active => HotbarSlotsConfig.Enabled.Value;

        private static ConfigEntry<string> CellEntry(int slot) => slot == 0 ? HotbarSlotsConfig.Slot9Cell : HotbarSlotsConfig.Slot10Cell;

        /// <summary>Case de la grille de l'emplacement supplémentaire <paramref name="slot"/> (0 ou 1).</summary>
        public static Vector2i Cell(int slot)
        {
            var parts = CellEntry(slot).Value.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int x) && int.TryParse(parts[1].Trim(), out int y) && y > 0)
                return new Vector2i(x, y);
            return new Vector2i(slot, 1);
        }

        /// <summary>Emplacement supplémentaire visant la case, ou -1.</summary>
        public static int SlotOf(Vector2i cell)
        {
            for (int slot = 0; slot < ExtraCount; slot++)
                if (Cell(slot) == cell)
                    return slot;
            return -1;
        }

        /// <summary>
        /// Nouvelle cible pour un emplacement ; si l'autre emplacement visait déjà cette case, ils s'échangent.
        /// Refusé sur la 1re ligne, déjà servie par les touches 1 à 8.
        /// </summary>
        public static void Rebind(Player player, int slot, Vector2i cell)
        {
            if (cell.y == 0)
            {
                player.Message(MessageHud.MessageType.Center, "La 1re ligne a déjà ses touches 1 à 8");
                return;
            }
            int other = SlotOf(cell);
            if (other >= 0 && other != slot)
                CellEntry(other).Value = CellEntry(slot).Value;
            CellEntry(slot).Value = cell.x + "," + cell.y;
            player.Message(MessageHud.MessageType.Center, $"Touche {Label(VanillaCount + slot)} : case {cell.x + 1} de la ligne {cell.y + 1}");
        }

        /// <summary>Objet de la case supplémentaire <paramref name="slot"/> (0 ou 1), ou null.</summary>
        public static ItemDrop.ItemData ItemIn(Inventory inventory, int slot)
        {
            var cell = Cell(slot);
            return inventory.GetItemAt(cell.x, cell.y);
        }

        /// <summary>Touche pressée ce frame pour un emplacement ajouté (1 = 9e case, 2 = 10e) ; 0 sinon.</summary>
        public static int PressedSlot()
        {
            if (IsPressed(HotbarSlotsConfig.Slot9Key.Value))
                return 1;
            if (IsPressed(HotbarSlotsConfig.Slot10Key.Value))
                return 2;
            return 0;
        }

        /// <summary>Lecture par <c>ZInput</c> plutôt que <c>KeyboardShortcut.IsDown</c> (Input System depuis la 1.0).</summary>
        private static bool IsPressed(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None || !ZInput.GetKeyDown(shortcut.MainKey))
                return false;
            foreach (var modifier in shortcut.Modifiers)
                if (!ZInput.GetKey(modifier))
                    return false;
            return true;
        }

        /// <summary>Étiquette de la case <paramref name="index"/> de la barre : « 1 » à « 8 », puis la touche configurée.</summary>
        public static string Label(int index)
        {
            if (index < VanillaCount)
                return (index + 1).ToString();
            var key = (index == VanillaCount ? HotbarSlotsConfig.Slot9Key : HotbarSlotsConfig.Slot10Key).Value.MainKey;
            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
                return ((int)(key - KeyCode.Alpha0)).ToString();
            if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
                return "N" + (int)(key - KeyCode.Keypad0);
            return key.ToString();
        }
    }
}
