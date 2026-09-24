using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Ovomium.Features.ServerWake
{
    /// <summary>
    /// Pastille d'état Nodecraft (« en hibernation », « démarrage… », « en ligne »…) ajoutée au nom des serveurs liés
    /// dans la liste, derrière un marqueur <c>&lt;link&gt;</c> invisible qui permet de la retirer. États relus toutes les
    /// 60 s et à chaque ouverture de la liste (GET seulement, sur une tâche d'arrière-plan), sauf pendant un réveil (la
    /// session publie l'état) ou une pause après un 429 (dernier état connu gardé) ; chaque état lu met à jour le nom
    /// mémorisé et, serveur en ligne, l'IP du favori (<see cref="LinkedFavorite"/>).
    /// </summary>
    internal static class ServerWakeBadges
    {
        private const string Marker = "<link=\"ovomium-wake\">";
        private const float RefreshSeconds = 60f;

        private static readonly object s_lock = new object();
        private static readonly Dictionary<string, NodecraftStatus> s_statuses = new Dictionary<string, NodecraftStatus>();
        private static bool s_dirty;
        private static volatile bool s_refreshing;
        private static float s_nextRefresh;
        private static int s_lastTickFrame = -10;

        /// <summary>Mémorise un état lu (tout thread) ; la liste sera redessinée à la prochaine frame.</summary>
        public static void Remember(string shareId, NodecraftStatus status)
        {
            lock (s_lock)
            {
                if (status.State == WakeState.RateLimited && s_statuses.ContainsKey(shareId))
                    return; // pause passagère : le dernier état connu reste affiché
                s_statuses[shareId] = status;
                s_dirty = true;
            }
        }

        /// <summary>Relit les états dès la prochaine frame (lien ajouté ou modifié).</summary>
        public static void RequestRefresh() => s_nextRefresh = 0f;

        /// <summary>Chaque frame de la liste des serveurs ouverte.</summary>
        public static void Tick(ServerListGui gui)
        {
            if (!ServerWakeConfig.Enabled.Value)
                return;
            if (Time.frameCount > s_lastTickFrame + 1)
                s_nextRefresh = 0f; // liste rouverte : relecture dès que les appels sont permis
            s_lastTickFrame = Time.frameCount;
            if (TakeDirty())
                Apply(gui);
            if (!s_refreshing && !ServerWakeSession.IsRunning && !NodecraftThrottle.IsPaused
                && Time.realtimeSinceStartup >= s_nextRefresh)
                Refresh(gui);
        }

        /// <summary>Postfix de <c>ServerListElement.UpdateDisplayData</c> : (re)pose la pastille de la ligne.</summary>
        public static void Decorate(ServerListElement row)
        {
            ServerLink link = ServerWakeConfig.Enabled.Value ? ServerLinkStore.Get(row.Server) : null;
            SetBadge(row, link == null ? null : Badge(Known(link.ShareId)?.State ?? WakeState.Unknown));
        }

        public static void Unload()
        {
            ServerListGui gui = ServerListGui.s_instance;
            if (gui == null)
                return;
            foreach (ServerListElement row in gui.m_serverListElements)
                SetBadge(row, null);
        }

        private static void SetBadge(ServerListElement row, string badge)
        {
            TMP_Text label = row.m_serverName;
            if (label == null)
                return;
            string text = label.text ?? "";
            int at = text.IndexOf(Marker, System.StringComparison.Ordinal);
            string wanted = (at >= 0 ? text.Substring(0, at) : text) + badge;
            if (wanted != text)
                label.text = wanted;
        }

        private static string Badge(WakeState state)
        {
            string label, color;
            switch (state)
            {
                case WakeState.Online: label = "en ligne"; color = "7CC47C"; break;
                case WakeState.Starting: label = "démarrage…"; color = "E6C35C"; break;
                case WakeState.Offline: label = "en hibernation"; color = "8FB3D9"; break;
                case WakeState.Locked: label = "partage verrouillé"; color = "D97A7A"; break;
                case WakeState.Unavailable: label = "archivé"; color = "D97A7A"; break;
                case WakeState.Error: label = "Nodecraft injoignable"; color = "D97A7A"; break;
                case WakeState.RateLimited: label = "Nodecraft en pause"; color = "9A9A9A"; break;
                default: label = "Nodecraft"; color = "9A9A9A"; break;
            }
            return $"{Marker}<size=75%><color=#{color}>  · {label}</color></size></link>";
        }

        private static NodecraftStatus Known(string shareId)
        {
            lock (s_lock)
                return s_statuses.TryGetValue(shareId, out NodecraftStatus status) ? status : null;
        }

        private static bool TakeDirty()
        {
            lock (s_lock)
            {
                bool dirty = s_dirty;
                s_dirty = false;
                return dirty;
            }
        }

        /// <summary>Nouveaux états : noms et adresses des favoris liés mis à jour, puis liste recalculée et redessinée.</summary>
        private static void Apply(ServerListGui gui)
        {
            foreach (ServerListElement row in new List<ServerListElement>(gui.m_serverListElements))
            {
                ServerLink link = ServerLinkStore.Get(row.Server);
                NodecraftStatus status = link == null ? null : Known(link.ShareId);
                if (status != null)
                    LinkedFavorite.Sync(row.Server, status);
            }
            gui.m_filteredListOutdated = true; // noms des entrées figés dans ServerListEntryData
            gui.UpdateServerListGui(false);
        }

        private static void Refresh(ServerListGui gui)
        {
            s_nextRefresh = Time.realtimeSinceStartup + RefreshSeconds;
            var shareIds = new HashSet<string>();
            foreach (ServerListElement row in gui.m_serverListElements)
            {
                ServerLink link = ServerLinkStore.Get(row.Server);
                if (link != null)
                    shareIds.Add(link.ShareId);
            }
            if (shareIds.Count == 0)
                return;
            s_refreshing = true;
            Task.Run(() =>
            {
                try
                {
                    foreach (string shareId in shareIds)
                        Remember(shareId, NodecraftClient.GetStatus(shareId));
                }
                finally
                {
                    s_refreshing = false;
                }
            });
        }
    }
}
