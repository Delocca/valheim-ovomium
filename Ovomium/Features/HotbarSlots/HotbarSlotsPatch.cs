using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Ovomium.Features.HotbarSlots
{
    /// <summary>
    /// Touches des cases 9 et 10, mêmes conditions que la boucle vanilla « Hotbar1..8 » (propriétaire et
    /// <c>TakeInput()</c>).
    /// </summary>
    [HarmonyPatch(typeof(Player), "Update", new System.Type[0])]
    internal static class HotbarSlotsPlayerUpdatePatch
    {
        private static void Postfix(Player __instance)
        {
            if (!HotbarSlots.Active || __instance.m_nview == null || !__instance.m_nview.IsValid()
                || !__instance.m_nview.IsOwner() || !__instance.TakeInput())
                return;
            int slot = HotbarSlots.PressedSlot();
            if (slot != 0)
                __instance.UseHotbarItem(HotbarSlots.VanillaCount + slot);
        }
    }

    /// <summary>
    /// Index 9 et 10 (touches, clic tactile sur la barre, manette) : l'objet vient des cases supplémentaires, pas de
    /// la 1re ligne.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UseHotbarItem), new[] { typeof(int) })]
    internal static class HotbarSlotsUsePatch
    {
        private static bool Prefix(Player __instance, int index)
        {
            int slot = index - 1 - HotbarSlots.VanillaCount;
            if (!HotbarSlots.Active || slot < 0 || slot >= HotbarSlots.ExtraCount)
                return true;
            var item = HotbarSlots.ItemIn(__instance.m_inventory, slot);
            if (item != null)
                __instance.UseItem(null, item, fromInventoryGui: false);
            return false;
        }
    }

    /// <summary>
    /// Barre d'objets : le temps d'<c>UpdateIcons</c>, les objets des cases supplémentaires sont projetés en (8,0) et
    /// (9,0) pour être dessinés comme 9e et 10e cases ; positions rendues ensuite (Finalizer : même si le jeu lève).
    /// Puis étiquettes des cases ajoutées et recentrage d'une case.
    /// </summary>
    [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons", new[] { typeof(Player) })]
    internal static class HotbarSlotsBarPatch
    {
        private static readonly List<ItemDrop.ItemData> s_projected = new List<ItemDrop.ItemData>(2);

        private static void Prefix(Player player)
        {
            s_projected.Clear();
            if (!HotbarSlots.Active || player == null)
                return;
            for (int slot = 0; slot < HotbarSlots.ExtraCount; slot++)
            {
                var item = HotbarSlots.ItemIn(player.m_inventory, slot);
                if (item == null)
                    continue;
                item.m_gridPos = new Vector2i(HotbarSlots.VanillaCount + slot, 0);
                s_projected.Add(item);
            }
        }

        private static void Finalizer(HotkeyBar __instance)
        {
            for (int i = 0; i < s_projected.Count; i++)
                s_projected[i].m_gridPos = HotbarSlots.Cell(s_projected[i].m_gridPos.x - HotbarSlots.VanillaCount);
            s_projected.Clear();
            Restyle(__instance);
        }

        private static void Restyle(HotkeyBar bar)
        {
            var elements = bar.m_elements;
            if (!HotbarSlots.Active || elements.Count <= HotbarSlots.VanillaCount)
                return;
            float offset = HotbarSlotsConfig.RecenterBar.Value
                ? -(elements.Count - HotbarSlots.VanillaCount) * bar.m_elementSpace / 2f : 0f;
            bool gamepad = ZInput.IsGamepadActive();
            for (int i = 0; i < elements.Count; i++)
            {
                var go = elements[i].m_go;
                go.transform.localPosition = new Vector3(i * bar.m_elementSpace + offset, 0f, 0f);
                if (i >= HotbarSlots.VanillaCount && !gamepad)
                    go.transform.Find("binding").GetComponent<TMP_Text>().text = HotbarSlots.Label(i);
            }
        }
    }

    /// <summary>
    /// Grille du joueur : étiquettes « 9 » et « 0 » sur les cases supplémentaires, recalculées chaque frame (la cible
    /// peut changer) : hors 1re ligne, seule la case visée porte une étiquette.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui", new[] { typeof(Player), typeof(ItemDrop.ItemData) })]
    internal static class HotbarSlotsGridPatch
    {
        private static void Postfix(InventoryGrid __instance, Player player)
        {
            if (!HotbarSlots.Active || player == null)
                return;
            int width = __instance.m_width;
            for (int i = width; i < __instance.m_elements.Count; i++)
            {
                int slot = HotbarSlots.SlotOf(new Vector2i(i % width, i / width));
                var binding = __instance.m_elements[i].transform.Find("binding").GetComponent<TMP_Text>();
                binding.enabled = slot >= 0;
                if (slot >= 0)
                    binding.text = HotbarSlots.Label(HotbarSlots.VanillaCount + slot);
            }
        }
    }

    /// <summary>Inventaire ouvert : la touche 9 ou 0 sur une case survolée de la grille joueur en fait la nouvelle cible.</summary>
    [HarmonyPatch(typeof(InventoryGui), "Update", new System.Type[0])]
    internal static class HotbarSlotsRebindPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            if (!HotbarSlots.Active || !InventoryGui.IsVisible() || Player.m_localPlayer == null)
                return;
            int slot = HotbarSlots.PressedSlot();
            if (slot == 0)
                return;
            var element = __instance.m_playerGrid.GetHoveredElement();
            if (element != null)
                HotbarSlots.Rebind(Player.m_localPlayer, slot - 1, element.Position);
        }
    }

    /// <summary>Menu radial : les deux cases supplémentaires à la suite des 8 de la 1re ligne.</summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetHotbar), new[] { typeof(bool) })]
    internal static class HotbarSlotsRadialPatch
    {
        private static void Postfix(Inventory __instance, bool includeEmpty, List<ItemDrop.ItemData> __result)
        {
            if (!HotbarSlots.Active || __result == null)
                return;
            for (int slot = 0; slot < HotbarSlots.ExtraCount; slot++)
            {
                var item = HotbarSlots.ItemIn(__instance, slot);
                if (item != null || includeEmpty)
                    __result.Add(item);
            }
        }
    }
}
