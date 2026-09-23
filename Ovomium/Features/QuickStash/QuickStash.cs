using Ovomium.Features.CraftFromChests;
using UnityEngine;

namespace Ovomium.Features.QuickStash
{
    /// <summary>
    /// Rangement d'un objet de l'inventaire ouvert vers le coffre le plus proche qui en contient déjà : choix du
    /// coffre parmi <see cref="NearbyChests"/>, transfert par <c>Inventory.AddItem(ItemData)</c> (la case est alors
    /// choisie par ChestFill), vol inverse de la case d'inventaire vers le coffre (ItemFlight). On n'écrit que dans un
    /// coffre à nous : sinon sa propriété est demandée et le rangement attend (<see cref="QuickStashQueue"/>).
    /// </summary>
    internal static class QuickStash
    {
        public static bool Active() =>
            QuickStashConfig.Enabled.Value && Player.m_localPlayer != null && !Player.m_localPlayer.IsTeleporting();

        /// <summary>
        /// Range <paramref name="amount"/> exemplaires de <paramref name="item"/> (pris dans <paramref name="source"/>)
        /// dans le coffre choisi, tout de suite s'il est à nous, sinon après réception de sa propriété ; vrai si le clic
        /// est pris en charge, faux si aucun coffre ne convient ou si le transfert échoue (rien n'a bougé).
        /// </summary>
        public static bool TryStash(InventoryGui gui, Inventory source, ItemDrop.ItemData item, int amount)
        {
            Player player = Player.m_localPlayer;
            if (source == null || item == null || amount <= 0 || !source.ContainsItem(item)) return false;
            if (QuickStashQueue.Contains(item)) return true;
            Container chest = FindTarget(player, source, item, amount);
            if (chest == null) return false;
            if (chest.m_nview.IsOwner()) return Stash(gui, source, item, amount, chest);
            QuickStashQueue.Enqueue(source, item, amount, chest);
            return true;
        }

        /// <summary>Transfert vers un coffre à nous, effet vanilla et vol ; faux si le transfert échoue (rien n'a bougé).</summary>
        internal static bool Stash(InventoryGui gui, Inventory source, ItemDrop.ItemData item, int amount, Container chest)
        {
            Player player = Player.m_localPlayer;
            Vector3 from = InventoryGui.IsVisible() ? CellWorldPoint(gui, source, item, player) : player.GetCenterPoint();
            if (source == player.GetInventory())
            {
                player.RemoveEquipAction(item);
                player.UnequipItem(item);
            }
            if (!Transfer(chest.GetInventory(), source, item, amount)) return false;
            float distance = Vector3.Distance(player.transform.position, chest.transform.position);
            Plugin.Log.LogInfo($"QuickStash : {amount} × {Localization.instance.Localize(item.m_shared.m_name)} → "
                + $"{chest.m_name} à {distance:0.0} m");
            gui.m_moveItemEffects.Create(gui.transform.position, Quaternion.identity);
            ItemDrop prefab = item.m_dropPrefab != null ? item.m_dropPrefab.GetComponent<ItemDrop>() : null;
            if (prefab == null) return true;
            ItemFlight.ItemFlight.LaunchToChest(prefab, amount, from, chest);
            ItemFlight.FlightBroadcast.SendToChest(player, prefab, amount, chest);
            return true;
        }

        /// <summary>Coffre le plus proche contenant déjà l'objet et pouvant accueillir la quantité, hors inventaire source.</summary>
        private static Container FindTarget(Player player, Inventory source, ItemDrop.ItemData item, int amount)
        {
            string name = item.m_shared.m_name;
            foreach (Container chest in NearbyChests.Find(player.transform.position))
            {
                Inventory inventory = chest.GetInventory();
                if (inventory == source) continue;
                if (inventory.ContainsItemByName(name) && inventory.CanAddItem(item, amount)) return chest;
            }
            return null;
        }

        /// <summary>
        /// Pile entière : <c>MoveItemToThis</c> vanilla (l'objet change d'inventaire) ; partie d'une pile : clone
        /// ajouté au coffre, puis seulement retrait de la source. Aucune perte : un ajout refusé laisse la source intacte.
        /// </summary>
        private static bool Transfer(Inventory chest, Inventory source, ItemDrop.ItemData item, int amount)
        {
            if (amount >= item.m_stack)
            {
                chest.MoveItemToThis(source, item);
                return !source.ContainsItem(item);
            }
            ItemDrop.ItemData part = item.Clone();
            part.m_stack = amount;
            if (!chest.AddItem(part)) return false;
            return source.RemoveItem(item, amount);
        }

        /// <summary>Point du monde à 1 m devant la caméra, sous la case d'inventaire de l'objet ; sinon le centre du joueur.</summary>
        private static Vector3 CellWorldPoint(InventoryGui gui, Inventory source, ItemDrop.ItemData item, Player player)
        {
            InventoryGrid grid = source == player.GetInventory() ? gui.m_playerGrid : gui.m_containerGrid;
            Camera camera = GameCamera.instance != null ? GameCamera.instance.m_camera : null;
            if (grid == null || camera == null) return player.GetCenterPoint();
            InventoryElement element = grid.GetElement(item.m_gridPos.x, item.m_gridPos.y, grid.m_width);
            var rect = element != null ? element.transform as RectTransform : null;
            if (rect == null) return player.GetCenterPoint();
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, rect.TransformPoint(rect.rect.center));
            return camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 1f));
        }
    }
}
