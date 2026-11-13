using System.Collections.Generic;
using HarmonyLib;

namespace Ovomium.Net
{
    /// <summary>
    /// Message routé entre joueuses du mod (<c>ZRoutedRpc</c>, paquet <see cref="ZPackage"/>). Envoyé à tous, il est
    /// relayé par le serveur dédié vanilla sans qu'il le connaisse (<c>RPC_RoutedRPC</c> → <c>RouteRPC</c>) ; une
    /// joueuse sans le mod l'ignore en silence (hash non enregistré) ; il est aussi exécuté chez l'expéditrice
    /// (<see cref="IsFromSelf"/>). <c>ZRoutedRpc</c> est recréé à chaque <c>ZNet.Awake</c> et n'a pas de
    /// désenregistrement, et son <c>Register</c> est un <c>Dictionary.Add</c> : le hash est retiré de
    /// <c>m_functions</c> à la main avant chaque enregistrement et au déchargement (rechargement à chaud).
    /// </summary>
    internal sealed class RoutedChannel
    {
        private static readonly List<RoutedChannel> s_installed = new List<RoutedChannel>();

        private readonly string m_name;
        private readonly int m_hash;
        private readonly System.Action<long, ZPackage> m_handler;

        public RoutedChannel(string name, System.Action<long, ZPackage> handler)
        {
            m_name = name;
            m_hash = name.GetStableHashCode();
            m_handler = handler;
        }

        /// <summary>Enregistre le message maintenant (rechargement en partie) et à chaque nouvelle session.</summary>
        public void Install()
        {
            if (!s_installed.Contains(this)) s_installed.Add(this);
            Register();
        }

        public void Unload()
        {
            s_installed.Remove(this);
            ZRoutedRpc.instance?.m_functions.Remove(m_hash);
        }

        /// <summary>Envoie à toutes les joueuses connectées, soi comprise.</summary>
        public void SendToEverybody(ZPackage pkg)
        {
            if (ZRoutedRpc.instance == null || ZNet.instance == null) return;
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, m_name, pkg);
        }

        public static bool IsFromSelf(long senderPeer) =>
            ZRoutedRpc.instance == null || senderPeer == ZRoutedRpc.instance.m_id;

        private void Register()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null) return;
            rpc.m_functions.Remove(m_hash);
            rpc.Register<ZPackage>(m_name, (sender, pkg) => m_handler(sender, pkg));
        }

        [HarmonyPatch(typeof(ZNet), "Awake", new System.Type[0])]
        private static class ZNetAwakePatch
        {
            private static void Postfix()
            {
                foreach (RoutedChannel channel in s_installed) channel.Register();
            }
        }
    }
}
