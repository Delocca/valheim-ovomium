using System.Collections.Generic;

namespace Ovomium.Features.AutoJoin
{
    /// <summary>
    /// Résolution du personnage et du serveur cibles. Les personnages sont ceux déjà chargés par
    /// FejdStartup.Start (m_profiles, index par défaut m_profileIndex). Les serveurs Favoris / Récents sont lus
    /// depuis leurs fichiers (LocalServerList, comme le fait le jeu quand ServerListGui n'est pas ouvert) ;
    /// le plus récent est en tête de la liste « recent ».
    /// </summary>
    internal static class AutoJoinTargets
    {
        /// <summary>Index dans <paramref name="profiles"/> du personnage demandé (nom ou fichier), -1 si absent.</summary>
        public static int FindProfile(List<PlayerProfile> profiles, string wanted, int defaultIndex)
        {
            if (profiles == null || profiles.Count == 0)
                return -1;
            if (string.IsNullOrEmpty(wanted))
                return defaultIndex >= 0 && defaultIndex < profiles.Count ? defaultIndex : 0;
            for (int i = 0; i < profiles.Count; i++)
            {
                if (Same(profiles[i].GetName(), wanted) || Same(profiles[i].GetFilename(), wanted))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Serveur demandé : vide = dernier serveur rejoint ; sinon nom connu des listes Favoris / Récents, ou
        /// adresse hôte[:port] (port 2456 par défaut). <c>ServerJoinData.None</c> si rien ne convient.
        /// </summary>
        public static ServerJoinData FindServer(string wanted)
        {
            var favorites = new LocalServerList(null, ServerListGui.GetServerListLocations("favorite"));
            var recent = new LocalServerList(null, ServerListGui.GetServerListLocations("recent"));
            try
            {
                if (string.IsNullOrEmpty(wanted))
                    return recent.Count > 0 ? recent[0] : ServerJoinData.None;
                ServerJoinData known = FindByName(favorites, wanted);
                if (!known.IsValid)
                    known = FindByName(recent, wanted);
                return known.IsValid ? known : ParseAddress(wanted);
            }
            finally
            {
                favorites.Dispose();
                recent.Dispose();
            }
        }

        private static ServerJoinData FindByName(LocalServerList list, string wanted)
        {
            for (int i = 0; i < list.Count; i++)
            {
                ServerJoinData data = list[i];
                if (Same(MultiBackendMatchmaking.GetServerName(data), wanted) || Same(data.ToString(), wanted))
                    return data;
            }
            return ServerJoinData.None;
        }

        private static ServerJoinData ParseAddress(string address)
        {
            var dedicated = new ServerJoinDataDedicated(address);
            return string.IsNullOrEmpty(dedicated.m_host) ? ServerJoinData.None : new ServerJoinData(dedicated);
        }

        private static bool Same(string a, string b)
        {
            return string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
