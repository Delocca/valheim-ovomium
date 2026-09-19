using System.Collections.Generic;
using UnityEngine;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Coffres utilisables autour d'un point : tout <c>Container</c> porté par une <c>Piece</c> (coffres, chariots,
    /// bateaux ; exclut tombes et coffres de donjon), accessible (privé, ward), pas en cours d'utilisation par une
    /// autre joueuse. Leur inventaire est rafraîchi par le jeu chaque seconde même sans possession (lecture fiable) ;
    /// l'écriture exige de posséder le ZDO : on reproduit la moitié « réponse » du protocole vanilla de « Tout prendre »
    /// (<c>Container.RPC_TakeAllResponse</c> : <c>ClaimOwnership</c> puis <c>ForceSendZDO</c> à l'ancien propriétaire).
    /// </summary>
    internal static class NearbyChests
    {
        private const float CacheMoveTolerance = 0.5f;
        private const float CacheSeconds = 0.5f;

        private static readonly List<Container> s_cache = new List<Container>();
        private static Vector3 s_cacheCenter;
        private static float s_cacheTime = -1f;
        private static float s_cacheRange;

        /// <summary>Coffres utilisables, du plus proche au plus loin (liste partagée : ne pas la modifier).</summary>
        public static List<Container> Find(Vector3 center)
        {
            float range = CraftFromChestsConfig.Range.Value;
            if (s_cacheTime >= 0f && Time.time - s_cacheTime < CacheSeconds && range == s_cacheRange
                && Vector3.Distance(center, s_cacheCenter) < CacheMoveTolerance)
                return s_cache;
            s_cache.Clear();
            var pieces = new List<Piece>();
            Piece.GetAllPiecesInRadius(center, range, pieces);
            long playerId = Game.instance.GetPlayerProfile().GetPlayerID();
            foreach (var piece in pieces)
            {
                var container = piece.GetComponentInChildren<Container>();
                if (container != null && IsUsable(container, playerId) && !s_cache.Contains(container))
                    s_cache.Add(container);
            }
            s_cache.Sort((a, b) => Vector3.Distance(center, a.transform.position)
                .CompareTo(Vector3.Distance(center, b.transform.position)));
            s_cacheCenter = center;
            s_cacheTime = Time.time;
            s_cacheRange = range;
            return s_cache;
        }

        private static bool IsUsable(Container container, long playerId)
        {
            var nview = container.m_nview;
            if (nview == null || !nview.IsValid() || container.GetInventory() == null) return false;
            if (container.m_privacy != Container.PrivacySetting.Public
                && (container.m_piece == null || !container.CheckAccess(playerId))) return false;
            if (container.m_checkGuardStone && !PrivateArea.CheckAccess(container.transform.position, 0f, false, false))
                return false;
            if (container.IsInUse() || nview.GetZDO().GetInt(ZDOVars.s_inUse) == 1) return false;
            if (container.m_wagon != null && container.m_wagon.InUse()) return false;
            return true;
        }

        /// <summary>Nombre d'exemplaires de <paramref name="name"/> dans les coffres (qualité -1 : toutes).</summary>
        public static int Count(Vector3 center, string name, int quality)
        {
            int total = 0;
            foreach (var container in Find(center))
                total += container.GetInventory().CountItems(name, quality);
            return total;
        }

        /// <summary>
        /// Prévoit, sans rien retirer, d'où viendraient jusqu'à <paramref name="amount"/> exemplaires (coffre le plus
        /// proche d'abord) ; ajoute les retraits prévus à <paramref name="pulls"/> et retourne le reliquat introuvable.
        /// </summary>
        public static int Plan(Vector3 center, ItemDrop item, int amount, int itemQuality, List<Pull> pulls)
        {
            string name = item.m_itemData.m_shared.m_name;
            foreach (var container in Find(center))
            {
                if (amount <= 0) break;
                int n = Mathf.Min(container.GetInventory().CountItems(name, itemQuality), amount);
                if (n <= 0) continue;
                pulls.Add(new Pull(container, item, n));
                amount -= n;
            }
            return amount;
        }

        /// <summary>
        /// Retire jusqu'à <paramref name="amount"/> exemplaires selon <see cref="Plan"/> ; retourne le reliquat et
        /// ajoute les retraits effectifs à <paramref name="taken"/> (facultatif).
        /// </summary>
        public static int Remove(Vector3 center, ItemDrop item, int amount, int itemQuality, List<Pull> taken = null)
        {
            var pulls = new List<Pull>();
            int missing = Plan(center, item, amount, itemQuality, pulls);
            string name = item.m_itemData.m_shared.m_name;
            foreach (var pull in pulls)
            {
                var container = pull.Chest;
                if (!container.m_nview.IsOwner())
                {
                    Plugin.Log.LogInfo($"CraftFromChests : repli, prise directe de {container.m_name} (non réservé)");
                    TakeOwnership(container);
                }
                int n = RemoveFrom(container.GetInventory(), name, pull.Amount, itemQuality);
                missing += pull.Amount - n;
                if (n > 0) taken?.Add(new Pull(container, item, n));
                if (CraftFromChestsConfig.LogPulls.Value)
                    Plugin.Log.LogInfo($"CraftFromChests : {n} × {Localization.instance.Localize(name)} pris dans "
                        + $"{container.m_name} à {Vector3.Distance(center, container.transform.position):0.0} m");
            }
            return missing;
        }

        /// <summary>Prise directe (moitié « réponse » de « Tout prendre ») : repli quand la réservation n'a pas abouti.</summary>
        internal static void TakeOwnership(Container container)
        {
            var nview = container.m_nview;
            if (nview.IsOwner()) return;
            long previous = nview.GetZDO().GetOwner();
            nview.ClaimOwnership();
            if (previous != 0L)
                ZDOMan.instance.ForceSendZDO(previous, nview.GetZDO().m_uid);
        }

        /// <summary>
        /// Même filtre que <c>Inventory.RemoveItem(string, int, int, bool)</c>, mais pile par pile de droite à gauche
        /// et de bas en haut : les premières cases restent stables. Retourne la quantité retirée.
        /// </summary>
        private static int RemoveFrom(Inventory inventory, string name, int amount, int itemQuality)
        {
            var stacks = new List<ItemDrop.ItemData>();
            foreach (var item in inventory.GetAllItems())
            {
                if (item.m_shared.m_name == name && (itemQuality < 0 || item.m_quality == itemQuality)
                    && item.m_worldLevel >= Game.m_worldLevel)
                    stacks.Add(item);
            }
            stacks.Sort((a, b) => a.m_gridPos.y != b.m_gridPos.y
                ? b.m_gridPos.y.CompareTo(a.m_gridPos.y)
                : b.m_gridPos.x.CompareTo(a.m_gridPos.x));
            int taken = 0;
            foreach (var stack in stacks)
            {
                int n = Mathf.Min(stack.m_stack, amount - taken);
                stack.m_stack -= n;
                taken += n;
                if (taken >= amount) break;
            }
            if (taken > 0)
            {
                inventory.m_inventory.RemoveAll(x => x.m_stack <= 0);
                inventory.Changed();
            }
            return taken;
        }
    }
}
