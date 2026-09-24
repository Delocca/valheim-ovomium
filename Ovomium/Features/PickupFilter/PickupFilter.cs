using System.Collections.Generic;

namespace Ovomium.Features.PickupFilter
{
    /// <summary>
    /// Types d'objets exclus du ramassage automatique, par personnage, identifiés par <c>m_shared.m_name</c> (même clé
    /// que l'empilement vanilla, présente au sol comme en inventaire). Liste gardée dans <c>Player.m_customData</c>
    /// (sauvegardée avec le profil, conservée à la mort) ; le cache n'est reparsé que si la valeur stockée change
    /// (autre personnage, profil rechargé).
    /// </summary>
    internal static class PickupFilter
    {
        private const string Key = "ovomium.pickupFilter";

        private static string s_raw;
        private static HashSet<string> s_names = new HashSet<string>();

        public static bool Active => PickupFilterConfig.Enabled.Value;

        /// <summary>
        /// Remplace la lecture de <c>ItemDrop.m_autoPickup</c> dans <c>Player.AutoPickup</c> (qui ne tourne que pour le
        /// joueur local) : un objet jeté par le joueur (<c>m_autoPickup</c> faux) reste exclu comme en vanilla.
        /// </summary>
        public static bool AllowsAutoPickup(ItemDrop drop) =>
            drop.m_autoPickup && !Ignores(drop.m_itemData.m_shared.m_name);

        /// <summary>Vrai si le joueur local a exclu ce type d'objet (toujours faux fonctionnalité désactivée).</summary>
        public static bool Ignores(string name)
        {
            Player player = Player.m_localPlayer;
            return Active && player != null && Names(player).Contains(name);
        }

        /// <summary>Exclut le type d'objet, ou le réautorise s'il l'était ; vrai s'il est désormais exclu.</summary>
        public static bool Toggle(Player player, string name)
        {
            HashSet<string> names = Names(player);
            bool ignored = !names.Remove(name);
            if (ignored)
                names.Add(name);
            string raw = PickupFilterList.Serialize(names);
            if (raw.Length == 0)
            {
                player.m_customData.Remove(Key);
                s_raw = null;
            }
            else
            {
                player.m_customData[Key] = raw;
                s_raw = raw;
            }
            return ignored;
        }

        private static HashSet<string> Names(Player player)
        {
            player.m_customData.TryGetValue(Key, out string raw);
            // Comparaison de références : une valeur relue du profil est une nouvelle chaîne.
            if (!ReferenceEquals(raw, s_raw))
            {
                s_raw = raw;
                s_names = PickupFilterList.Parse(raw);
            }
            return s_names;
        }
    }
}
