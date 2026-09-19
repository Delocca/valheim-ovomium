using Ovomium.Features.AutoJoin;

namespace Ovomium.Features.ContinueButton
{
    /// <summary>
    /// La dernière partie lancée. Le jeu mémorise le dernier monde hébergé (<c>PlatformPrefs</c> « world »), le
    /// dernier personnage (« profile ») et les serveurs récents (liste « recent », le dernier en tête), mais jamais
    /// si la dernière session était un monde local ou un serveur : c'est ce que le mod ajoute, sous la même forme.
    /// </summary>
    internal sealed class LastSession
    {
        private const string KindKey = "OvomiumLastSession";
        private const string KindWorld = "world";
        private const string KindServer = "server";

        public bool IsServer { get; }
        public string Name { get; }
        /// <summary>Monde local à démarrer (nul si serveur).</summary>
        public World World { get; }
        /// <summary>Serveur à rejoindre (<c>ServerJoinData.None</c> si monde local).</summary>
        public ServerJoinData Server { get; }

        private LastSession(World world)
        {
            World = world;
            Server = ServerJoinData.None;
            Name = world.m_name;
        }

        private LastSession(ServerJoinData server)
        {
            IsServer = true;
            Server = server;
            Name = MultiBackendMatchmaking.GetServerName(server);
        }

        /// <summary>Appelé quand le menu principal part vers la partie : mémorise le type de session.</summary>
        public static void Remember(FejdStartup startup)
        {
            PlatformPrefs.SetString(KindKey, startup.m_startingWorld ? KindWorld : KindServer);
        }

        /// <summary>Mod chargé en pleine partie (rechargement à chaud) : mémorise la session en cours.</summary>
        public static void RememberCurrent()
        {
            if (ZNet.instance != null)
                PlatformPrefs.SetString(KindKey, ZNet.instance.IsServer() ? KindWorld : KindServer);
        }

        /// <summary>La partie à continuer, ou nul si rien n'est mémorisé ou si la cible a disparu.</summary>
        public static LastSession Find()
        {
            string kind = PlatformPrefs.GetString(KindKey);
            if (kind == KindServer)
            {
                ServerJoinData server = AutoJoinTargets.FindServer("");
                return server.IsValid ? new LastSession(server) : null;
            }
            if (kind == KindWorld)
            {
                World world = FindWorld(PlatformPrefs.GetString("world"));
                return world != null ? new LastSession(world) : null;
            }
            return null;
        }

        private static World FindWorld(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            foreach (World world in SaveSystem.GetWorldList())
                if (world.m_name == name)
                    return world;
            return null;
        }
    }
}
