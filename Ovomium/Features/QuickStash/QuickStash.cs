using System.Collections.Generic;
using Ovomium.Features.CraftFromChests;
using UnityEngine;

namespace Ovomium.Features.QuickStash
{
    /// <summary>
    /// Rangement d'un objet de l'inventaire ouvert vers les coffres proches qui en contiennent déjà : répartition du
    /// plus proche au plus loin parmi <see cref="NearbyChests"/> (chacun reçoit ce qui y rentre, le reste reste en
    /// inventaire), transfert par <c>Inventory.AddItem(ItemData)</c> (la case est alors choisie par ChestFill), un vol
    /// inverse de la case d'inventaire vers chaque coffre (ItemFlight). On n'écrit que dans un coffre à nous : sinon sa
    /// propriété est demandée et sa part attend (<see cref="QuickStashQueue"/>).
    /// </summary>
    internal static class QuickStash
    {
        public static bool Active() =>
            QuickStashConfig.Enabled.Value && Player.m_localPlayer != null && !Player.m_localPlayer.IsTeleporting();

        /// <summary>
        /// Range <paramref name="amount"/> exemplaires de <paramref name="item"/> (pris dans <paramref name="source"/>)
        /// dans les coffres choisis, chaque part tout de suite si le coffre est à nous, sinon après réception de sa
        /// propriété ; vrai si le clic est pris en charge, faux si aucun coffre ne convient ou si tous les transferts
        /// échouent (rien n'a bougé).
        /// </summary>
        public static bool TryStash(InventoryGui gui, Inventory source, ItemDrop.ItemData item, int amount)
        {
            Player player = Player.m_localPlayer;
            if (source == null || item == null || amount <= 0 || !source.ContainsItem(item)) return false;
            if (QuickStashQueue.Contains(item)) return true;
            int handled = 0;
            foreach (KeyValuePair<Container, int> part in Plan(player, source, item, amount))
            {
                Container chest = part.Key;
                if (!chest.m_nview.IsOwner()) QuickStashQueue.Enqueue(source, item, part.Value, chest);
                else if (!Stash(gui, source, item, part.Value, chest)) continue;
                handled += part.Value;
            }
            if (handled == 0) return false;
            if (handled < amount)
            {
                Plugin.Log.LogInfo($"QuickStash : coffres pleins, {amount - handled} × {Name(item)} reste(nt) en inventaire");
                Full();
            }
            return true;
        }

        /// <summary>Transfert vers un coffre à nous, effet vanilla et vol ; faux si le transfert échoue (rien n'a bougé).</summary>
        internal static bool Stash(InventoryGui gui, Inventory source, ItemDrop.ItemData item, int amount, Container chest)
        {
            Player player = Player.m_localPlayer;
            Vector3 from = InventoryGui.IsVisible() ? CellWorldPoint(gui, source, item, player) : player.GetCenterPoint();
            if (source == player.GetInventory() && amount >= item.m_stack)
            {
                player.RemoveEquipAction(item);
                player.UnequipItem(item);
            }
            if (!Transfer(chest.GetInventory(), source, item, amount)) return false;
            float distance = Vector3.Distance(player.transform.position, chest.transform.position);
            Plugin.Log.LogInfo($"QuickStash : {amount} × {Name(item)} → {chest.m_name} à {distance:0.0} m");
            gui.m_moveItemEffects.Create(gui.transform.position, Quaternion.identity);
            ItemDrop prefab = item.m_dropPrefab != null ? item.m_dropPrefab.GetComponent<ItemDrop>() : null;
            if (prefab == null) return true;
            ItemFlight.ItemFlight.LaunchToChest(prefab, amount, from, chest);
            ItemFlight.FlightBroadcast.SendToChest(player, prefab, amount, chest);
            return true;
        }

        /// <summary>
        /// Parts de la pile par coffre : coffres contenant déjà l'objet (hors inventaire source), du plus proche au plus
        /// loin, chacun avec ce qui y rentre ; la somme peut être inférieure à <paramref name="amount"/>.
        /// </summary>
        private static List<KeyValuePair<Container, int>> Plan(Player player, Inventory source, ItemDrop.ItemData item, int amount)
        {
            var plan = new List<KeyValuePair<Container, int>>();
            foreach (Container chest in NearbyChests.Find(player.transform.position))
            {
                if (amount <= 0) break;
                Inventory inventory = chest.GetInventory();
                if (inventory == source || !inventory.ContainsItemByName(item.m_shared.m_name)) continue;
                int part = Mathf.Min(amount, Room(inventory, item));
                if (part <= 0) continue;
                plan.Add(new KeyValuePair<Container, int>(chest, part));
                amount -= part;
            }
            return plan;
        }

        /// <summary>
        /// Exemplaires de <paramref name="item"/> que l'inventaire peut encore recevoir par <c>AddItem</c> : cases vides
        /// plus place libre des piles de même nom, qualité et niveau de monde (<c>CanAddItem</c> ignore la qualité et
        /// peut surestimer : un ajout interrompu à mi-course dupliquerait la part déjà empilée).
        /// </summary>
        internal static int Room(Inventory inventory, ItemDrop.ItemData item)
        {
            int max = item.m_shared.m_maxStackSize;
            int room = inventory.GetEmptySlots() * max;
            foreach (ItemDrop.ItemData other in inventory.GetAllItems())
            {
                if (other.m_shared.m_name == item.m_shared.m_name && other.m_quality == item.m_quality
                    && other.m_worldLevel == item.m_worldLevel)
                    room += Mathf.Max(0, max - other.m_stack);
            }
            return room;
        }

        internal static void Full() => Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_itsfull");

        internal static string Name(ItemDrop.ItemData item) => Localization.instance.Localize(item.m_shared.m_name);

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
