using System.Collections.Generic;
using Ovomium.Features.PasswordReveal;

namespace Ovomium.Features.ServerWake
{
    /// <summary>
    /// Liens Nodecraft des favoris, clé = « IP:port » du favori (fichier des favoris 100 % vanilla). Chiffrés comme les
    /// mots de passe (<see cref="SecretFile"/>) : l'UUID permet de démarrer le serveur. Tout le fichier est lu une fois
    /// puis gardé en mémoire : la liste Communauté interroge des milliers de serveurs (<c>TryGetServerName</c>).
    /// Quand l'IP change, <see cref="LinkedFavorite"/> déplace le lien avec le favori (<see cref="Move"/>).
    /// </summary>
    internal static class ServerLinkStore
    {
        private static readonly SecretFile s_file = new SecretFile("ovo.ovomium.serverlinks.txt", "ServerWake");
        private static Dictionary<string, ServerLink> s_links;

        /// <summary>Clé d'un serveur, ou null s'il n'est pas un serveur dédié (seuls concernés par Nodecraft).</summary>
        public static string KeyOf(ServerJoinData server)
        {
            return server.IsValid && server.m_type == ServerJoinDataType.Dedicated ? server.Dedicated.ToString() : null;
        }

        /// <summary>Lien de ce serveur, ou null.</summary>
        public static ServerLink Get(ServerJoinData server)
        {
            string key = KeyOf(server);
            return key != null && Links.TryGetValue(key, out ServerLink link) ? link : null;
        }

        /// <summary>Pose le lien, ou le retire si <paramref name="link"/> est null.</summary>
        public static void Set(ServerJoinData server, ServerLink link)
        {
            string key = KeyOf(server);
            if (key == null)
                return;
            if (link == null)
            {
                if (Links.Remove(key))
                    s_file.Remove(key);
                return;
            }
            Links[key] = link;
            s_file.Set(key, link.Serialize());
        }

        /// <summary>Déplace le lien d'un serveur vers sa nouvelle adresse.</summary>
        public static void Move(ServerJoinData from, ServerJoinData to)
        {
            ServerLink link = Get(from);
            if (link == null)
                return;
            Set(to, link);
            Set(from, null);
        }

        private static Dictionary<string, ServerLink> Links
        {
            get
            {
                if (s_links != null)
                    return s_links;
                s_links = new Dictionary<string, ServerLink>();
                foreach (KeyValuePair<string, string> entry in s_file.GetAll())
                    s_links[entry.Key] = ServerLink.Deserialize(entry.Value);
                return s_links;
            }
        }
    }
}
