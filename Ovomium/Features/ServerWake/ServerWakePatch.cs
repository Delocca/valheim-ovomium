using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.ServerWake
{
    /// <summary>
    /// Ancrages du réveil Nodecraft : <c>FejdStartup.JoinServer</c> (point de passage de toutes les jonctions, reporté
    /// par <see cref="ServerWakeSession"/>), <c>ServerListGui.OnAddServer</c> / <c>OnRemoveServerConfirm</c> (favori
    /// ajouté depuis un lien, lien retiré avec le favori), <c>ServerListGui.UpdateButtons</c> (chaque frame, liste
    /// ouverte : états relus), <c>ServerListElement.UpdateDisplayData</c> (pastille d'état) et
    /// <c>MultiBackendMatchmaking.TryGetServerName</c> (nom Nodecraft au lieu de l'IP partout dans les menus).
    /// </summary>
    internal static class ServerWakePatch
    {
        public static void Unload()
        {
            ServerWakeSession.Unload();
            ServerWakeBadges.Unload();
        }

        [HarmonyPatch(typeof(FejdStartup), "JoinServer", new System.Type[0])]
        private static class JoinServerPatch
        {
            private static bool Prefix(FejdStartup __instance) => ServerWakeSession.AllowJoin(__instance);
        }

        [HarmonyPatch(typeof(ServerListGui), "OnAddServer", new System.Type[0])]
        private static class AddServerPatch
        {
            private static bool Prefix(ServerListGui __instance) => ServerWakeAdd.TryHandle(__instance);
        }

        [HarmonyPatch(typeof(ServerListGui), "OnRemoveServerConfirm", new System.Type[0])]
        private static class RemoveServerPatch
        {
            private static void Prefix(ServerListGui __instance, out ServerJoinData __state)
            {
                int selected = __instance.GetSelectedServer();
                __state = selected >= 0 ? __instance.CurrentServerListFiltered[selected].m_joinData : ServerJoinData.None;
            }

            private static void Postfix(ServerListGui __instance, ServerJoinData __state) => ServerWakeAdd.OnRemoved(__instance, __state);
        }

        [HarmonyPatch(typeof(ServerListGui), "UpdateButtons", new System.Type[0])]
        private static class UpdateButtonsPatch
        {
            private static void Postfix(ServerListGui __instance) => ServerWakeBadges.Tick(__instance);
        }

        [HarmonyPatch(typeof(ServerListElement), "UpdateDisplayData",
            new[] { typeof(ServerListEntryData), typeof(bool), typeof(RectTransform), typeof(ConnectIcons) },
            new[] { ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref })]
        private static class UpdateDisplayDataPatch
        {
            private static void Postfix(ServerListElement __instance) => ServerWakeBadges.Decorate(__instance);
        }

        /// <summary>
        /// Surcharge à trois arguments, appelée par toutes les autres (<c>GetServerName</c>, liste, recherche, sauvegarde
        /// des favoris) : un favori lié porte le nom Nodecraft, même serveur en hibernation (sans réponse au ping vanilla).
        /// </summary>
        [HarmonyPatch(typeof(MultiBackendMatchmaking), "TryGetServerName",
            new[] { typeof(ServerJoinData), typeof(ServerNameAtTimePoint), typeof(ServerNameSource) },
            new[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out })]
        private static class ServerNamePatch
        {
            private static void Postfix(ServerJoinData server, ref ServerNameAtTimePoint serverNameAtTimePoint,
                ref ServerNameSource source, ref bool __result)
            {
                if (!ServerWakeConfig.Enabled.Value)
                    return;
                ServerLink link = ServerLinkStore.Get(server);
                if (link == null || link.Name.Length == 0)
                    return;
                serverNameAtTimePoint = new ServerNameAtTimePoint(link.Name, System.DateTime.UtcNow);
                source = ServerNameSource.ManuallySet;
                __result = true;
            }
        }
    }
}
