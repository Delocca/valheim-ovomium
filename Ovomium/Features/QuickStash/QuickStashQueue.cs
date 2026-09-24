using System.Collections.Generic;
using Ovomium.Features.CraftFromChests;
using UnityEngine;

namespace Ovomium.Features.QuickStash
{
    /// <summary>
    /// Rangements en attente de la propriété du coffre cible, demandée au clic (<c>ChestReservation.RequestNow</c>) ;
    /// l'objet reste dans l'inventaire. Une pile répartie entre plusieurs coffres a une attente par coffre. Chaque frame,
    /// dans l'ordre des clics : coffre reçu → revalidation (objet encore là, place) puis rangement de ce qui rentre ; refus de la propriétaire ou 1,5 s sans propriété → message vanilla « Utilisé par
    /// quelqu'un d'autre », rien ne bouge. Le coffre est gardé tant que l'attente dure (garde courte renouvelée à chaque
    /// frame, qui s'éteint d'elle-même après le rangement) : une demande concurrente arrivée juste après la propriété est
    /// refusée au lieu de nous la reprendre avant le rangement.
    /// </summary>
    internal static class QuickStashQueue
    {
        private const float Timeout = 1.5f;
        private const float HoldRenew = 0.5f;

        private sealed class Pending
        {
            public Inventory Source;
            public ItemDrop.ItemData Item;
            public int Amount;
            public Container Chest;
            public float Since;
        }

        private static readonly List<Pending> s_pending = new List<Pending>();

        public static bool Contains(ItemDrop.ItemData item) => s_pending.Exists(p => p.Item == item);

        public static void Enqueue(Inventory source, ItemDrop.ItemData item, int amount, Container chest)
        {
            ChestReservation.RequestNow(chest);
            ChestReservation.Hold(chest, HoldRenew);
            s_pending.Add(new Pending { Source = source, Item = item, Amount = amount, Chest = chest, Since = Time.time });
            Plugin.Log.LogInfo($"QuickStash : {amount} × {Name(item)} en attente de {chest.m_name} (propriété demandée)");
        }

        /// <summary>Chaque frame (<c>InventoryGui.Update</c>) : règle les attentes arrivées à terme.</summary>
        public static void Tick(InventoryGui gui)
        {
            if (s_pending.Count == 0) return;
            if (!QuickStash.Active())
            {
                Plugin.Log.LogInfo($"QuickStash : {s_pending.Count} rangement(s) en attente abandonné(s)");
                s_pending.Clear();
                return;
            }
            for (int i = 0; i < s_pending.Count; i++)
            {
                if (!Resolve(gui, s_pending[i]))
                {
                    ChestReservation.Hold(s_pending[i].Chest, HoldRenew);
                    continue;
                }
                s_pending.RemoveAt(i);
                i--;
            }
        }

        /// <summary>Vrai si l'attente est terminée : rangé, échoué ou abandonné.</summary>
        private static bool Resolve(InventoryGui gui, Pending p)
        {
            ZNetView nview = p.Chest != null ? p.Chest.m_nview : null;
            if (nview == null || !nview.IsValid() || !p.Source.ContainsItem(p.Item))
            {
                Plugin.Log.LogInfo($"QuickStash : {Name(p.Item)} en attente abandonné (objet ou coffre disparu)");
                return true;
            }
            float waited = Time.time - p.Since;
            if (nview.IsOwner())
            {
                int wanted = Mathf.Min(p.Amount, p.Item.m_stack);
                int amount = Mathf.Min(wanted, QuickStash.Room(p.Chest.GetInventory(), p.Item));
                if (amount <= 0 || !QuickStash.Stash(gui, p.Source, p.Item, amount, p.Chest)) amount = 0;
                else Plugin.Log.LogInfo($"QuickStash : rangé après {waited:0.00} s d'attente de propriété");
                if (amount == wanted) return true;
                Plugin.Log.LogInfo($"QuickStash : {p.Chest.m_name} plein à la réception, "
                    + $"{wanted - amount} × {Name(p.Item)} reste(nt) en inventaire");
                QuickStash.Full();
                return true;
            }
            bool refused = ChestReservation.RefusedSince(p.Chest, p.Since);
            if (!refused && waited <= Timeout) return false;
            Plugin.Log.LogInfo($"QuickStash : {Name(p.Item)} non rangé, {p.Chest.m_name} "
                + (refused ? "refusé par sa propriétaire" : $"toujours pas à nous après {Timeout} s"));
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse");
            return true;
        }

        private static string Name(ItemDrop.ItemData item) => QuickStash.Name(item);

        /// <summary>Rechargement à chaud.</summary>
        public static void Unload() => s_pending.Clear();
    }
}
