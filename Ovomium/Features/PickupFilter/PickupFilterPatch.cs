using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Ovomium.Features.PickupFilter
{
    /// <summary>
    /// Ramassage automatique : l'unique lecture de <c>ItemDrop.m_autoPickup</c> (condition d'exclusion d'un objet
    /// proche) devient <see cref="PickupFilter.AllowsAutoPickup"/>, même effet de pile (ItemDrop → bool). Lecture
    /// introuvable ou multiple (mise à jour du jeu) : le patch échoue et l'autocontrôle le signale.
    /// </summary>
    [HarmonyPatch(typeof(Player), "AutoPickup", new[] { typeof(float) })]
    internal static class PickupFilterAutoPickupPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo flag = AccessTools.Field(typeof(ItemDrop), nameof(ItemDrop.m_autoPickup));
            MethodInfo filter = AccessTools.Method(typeof(PickupFilter), nameof(PickupFilter.AllowsAutoPickup),
                new[] { typeof(ItemDrop) });
            var code = new List<CodeInstruction>(instructions);
            int replaced = 0;
            foreach (CodeInstruction instruction in code)
            {
                if (!instruction.LoadsField(flag))
                    continue;
                instruction.opcode = OpCodes.Call;
                instruction.operand = filter;
                replaced++;
            }
            if (replaced != 1)
                throw new System.InvalidOperationException($"{replaced} lecture(s) de ItemDrop.m_autoPickup au lieu d'une");
            return code;
        }
    }

    /// <summary>
    /// Inventaire ouvert, la touche du ramassage automatique (V) sur un objet survolé (inventaire ou coffre) exclut ou
    /// réautorise son type. Pas de double effet : la bascule vanilla (<c>Player.Update</c>) exige <c>TakeInput()</c>,
    /// faux tant que <c>InventoryGui.IsVisible()</c>. Clavier seulement (la manette passe par « JoyAutoPickup »).
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "Update", new System.Type[0])]
    internal static class PickupFilterTogglePatch
    {
        private const string Button = "AutoPickup";

        private static void Postfix(InventoryGui __instance)
        {
            Player player = Player.m_localPlayer;
            if (!PickupFilter.Active || player == null || !ZInput.GetButtonDown(Button) || !InventoryGui.IsVisible()
                || Typing())
                return;
            ItemDrop.ItemData item = HoveredItem(__instance);
            if (item != null)
                ToggleAndTell(player, item.m_shared.m_name);
        }

        /// <summary>Bascule le type d'objet et l'annonce en haut à gauche (inventaire comme sol).</summary>
        internal static void ToggleAndTell(Player player, string name)
        {
            bool ignored = PickupFilter.Toggle(player, name);
            MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft,
                Localization.instance.Localize(name) + " : ramassage auto " + (ignored ? "désactivé" : "réactivé"));
        }

        private static bool Typing() =>
            (Chat.instance != null && Chat.instance.HasFocus()) || Console.IsVisible() || TextInput.IsVisible();

        /// <summary>Objet sous la souris dans la grille joueur, sinon dans celle du coffre s'il est ouvert.</summary>
        private static ItemDrop.ItemData HoveredItem(InventoryGui gui)
        {
            InventoryGrid grid = gui.m_playerGrid;
            InventoryElement element = grid.GetHoveredElement();
            if (element == null && gui.m_containerGrid.gameObject.activeInHierarchy)
            {
                grid = gui.m_containerGrid;
                element = grid.GetHoveredElement();
            }
            return element == null ? null : grid.GetInventory().GetItemAt(element.Position.x, element.Position.y);
        }
    }

    /// <summary>
    /// Au sol, V en visant un objet bascule son type au lieu du ramassage auto global. Prefix : <c>Player.Update</c>
    /// lit la bascule vanilla plus loin dans la même frame ; l'appui consommé (<c>ResetButtonStatus</c>), elle ne
    /// voit rien. <c>m_hovering</c> est celui de la frame précédente (<c>UpdateHover</c> tourne après ce Prefix).
    /// </summary>
    [HarmonyPatch(typeof(Player), "Update", new System.Type[0])]
    internal static class PickupFilterGroundTogglePatch
    {
        private const string Button = "AutoPickup";

        private static void Prefix(Player __instance)
        {
            if (!PickupFilter.Active || __instance != Player.m_localPlayer || __instance.m_hovering == null
                || !ZInput.GetButtonDown(Button) || !__instance.TakeInput())
                return;
            ItemDrop drop = __instance.m_hovering.GetComponentInParent<ItemDrop>();
            if (drop == null || drop.IsPiece())
                return;
            ZInput.ResetButtonStatus(Button);
            PickupFilterTogglePatch.ToggleAndTell(__instance, drop.m_itemData.m_shared.m_name);
        }
    }

    /// <summary>Badge sur chaque case (inventaire et coffre) dont l'objet est exclu, recalculé à chaque rafraîchissement.</summary>
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui", new[] { typeof(Player), typeof(ItemDrop.ItemData) })]
    internal static class PickupFilterGridPatch
    {
        private static void Postfix(InventoryGrid __instance)
        {
            Inventory inventory = __instance.m_inventory;
            foreach (InventoryElement element in __instance.m_elements)
            {
                ItemDrop.ItemData item = element.m_used ? inventory.GetItemAt(element.Position.x, element.Position.y) : null;
                PickupFilterBadge.Show(element, item != null && PickupFilter.Ignores(item.m_shared.m_name));
            }
        }
    }

    /// <summary>
    /// Au sol, l'infobulle propose la bascule du filtre pour ce type d'objet, au format des touches vanilla
    /// (<c>$KEY_&lt;bouton&gt;</c> remplacé par la touche assignée).
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverText), new System.Type[0])]
    internal static class PickupFilterHoverPatch
    {
        private static void Postfix(ItemDrop __instance, ref string __result)
        {
            if (!PickupFilter.Active || __instance.IsPiece())
                return;
            string action = PickupFilter.Ignores(__instance.m_itemData.m_shared.m_name) ? "Réactiver" : "Désactiver";
            __result += Localization.instance.Localize(
                "\n[<color=yellow><b>$KEY_AutoPickup</b></color>] " + action + " le ramassage auto");
        }
    }
}
