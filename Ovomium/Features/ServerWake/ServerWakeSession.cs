using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Ovomium.Features.ServerWake
{
    /// <summary>
    /// Réveil d'un serveur Nodecraft avant la jonction. <c>FejdStartup.JoinServer</c> (point de passage de tous les
    /// chemins : bouton Rejoindre, double-clic, AutoJoin, Continuer) est reporté tant que le serveur lié n'est pas prêt :
    /// état lu toutes les 10 s (comme la page web), démarrage demandé s'il est arrêté (une seule fois, y compris d'une
    /// attente annulée à la suivante tant que dure <c>MaxWaitMinutes</c>), puis jonction dès que Nodecraft annonce
    /// <c>status=online</c> (il distingue déjà <c>started</c>, processus lancé, d'<c>online</c>, monde chargé ; le
    /// serveur, crossplay, ne répond pas aux requêtes Steam A2S). Un 429 n'interrompt jamais l'attente : pause de
    /// <see cref="NodecraftThrottle"/> affichée, puis reprise. Réseau sur des tâches d'arrière-plan, attente dans une
    /// coroutine de FejdStartup.
    /// </summary>
    internal static class ServerWakeSession
    {
        private const float PollSeconds = 10f;
        private const int MaxConsecutiveErrors = 3;

        /// <summary>Instant (realtimeSinceStartup) du dernier démarrage accepté par Nodecraft, par lien.</summary>
        private static readonly Dictionary<string, float> s_startedAt = new Dictionary<string, float>();

        private static FejdStartup s_startup;
        private static ServerJoinData s_server;
        private static string s_shareId;
        private static string s_name;
        private static string s_phase;
        private static float s_startTime;
        private static Coroutine s_routine;
        private static PopupBase s_popup;
        private static NodecraftStatus s_result;
        /// <summary>Dernier couple (status, jit_status) journalisé : une ligne par changement seulement.</summary>
        private static string s_loggedState;
        /// <summary>Serveur autorisé à passer le prochain JoinServer (celui qu'on relance nous-mêmes).</summary>
        private static ServerJoinData s_bypass;

        /// <summary>Attente en cours : les pastilles ne relisent pas l'état (la session le publie).</summary>
        public static bool IsRunning => s_routine != null && s_startup != null;

        /// <summary>Prefix de JoinServer : faux si la jonction est reportée le temps du réveil.</summary>
        public static bool AllowJoin(FejdStartup startup)
        {
            ServerJoinData server = startup.GetServerToJoin();
            if (s_bypass.IsValid && s_bypass == server)
            {
                s_bypass = ServerJoinData.None;
                return true;
            }
            if (!ServerWakeConfig.Enabled.Value)
                return true;
            ServerLink link = ServerLinkStore.Get(server);
            if (link == null)
                return true;
            if (!IsRunning)
                Begin(startup, server, link.ShareId);
            return false;
        }

        public static void Unload()
        {
            Stop();
        }

        private static void Begin(FejdStartup startup, ServerJoinData server, string shareId)
        {
            s_startup = startup;
            s_server = server;
            s_shareId = shareId;
            s_name = MultiBackendMatchmaking.GetServerName(server);
            s_phase = "Vérification de l'état du serveur…";
            s_startTime = Time.realtimeSinceStartup;
            s_loggedState = null;
            Plugin.Log.LogInfo($"ServerWake : jonction à {s_name} ({server}) reportée, vérification Nodecraft");
            s_popup = ServerWakePopups.ShowWaiting(Header, Body, Cancel);
            s_routine = startup.StartCoroutine(Run());
        }

        private static string Header() => $"Réveil de {s_name}";

        private static string Body()
        {
            int seconds = (int)(Time.realtimeSinceStartup - s_startTime);
            int paused = NodecraftThrottle.SecondsLeft;
            string phase = paused > 0 ? $"Nodecraft limite les requêtes, nouvel essai dans {paused} s" : s_phase;
            return $"{phase}\n\n{seconds / 60}:{seconds % 60:00}";
        }

        private static int Elapsed => (int)(Time.realtimeSinceStartup - s_startTime);

        private static float StartMemorySeconds => ServerWakeConfig.MaxWaitMinutes.Value * 60f;

        private static IEnumerator Run()
        {
            float deadline = Time.realtimeSinceStartup + StartMemorySeconds;
            bool waiting = false, startSent = StartedRecently();
            int errors = 0;
            while (true)
            {
                while (NodecraftThrottle.IsPaused && Time.realtimeSinceStartup <= deadline)
                    yield return new WaitForSecondsRealtime(0.5f);
                yield return Fetch(() => NodecraftClient.GetStatus(s_shareId));
                NodecraftStatus status = s_result;
                if (status.State != WakeState.RateLimited)
                    Observe(status);
                if (status.State == WakeState.Online)
                {
                    Finish();
                    yield break;
                }
                if (status.State == WakeState.Offline && !startSent)
                {
                    yield return RequestStart();
                    if (s_result.State != WakeState.RateLimited && !s_result.Success)
                    {
                        Fail($"Nodecraft a refusé le démarrage : {s_result.Message}");
                        yield break;
                    }
                    startSent = s_result.Success;
                }
                if (status.State != WakeState.RateLimited)
                    errors = status.State == WakeState.Error ? errors + 1 : 0;
                string failure = FailureOf(status, waiting || startSent, errors) ?? (Time.realtimeSinceStartup > deadline
                    ? $"Le serveur n'est toujours pas prêt après {ServerWakeConfig.MaxWaitMinutes.Value} min." : null);
                if (failure != null)
                {
                    Fail(failure);
                    yield break;
                }
                waiting = true;
                if (status.Status == "started")
                    s_phase = "Serveur lancé, chargement du monde…";
                else if (startSent || status.State == WakeState.Starting)
                    s_phase = "Démarrage du serveur…";
                if (!NodecraftThrottle.IsPaused)
                    yield return new WaitForSecondsRealtime(PollSeconds);
            }
        }

        /// <summary>Démarrage déjà accepté pour ce lien pendant une attente récente (annulée) : ne pas le redemander.</summary>
        private static bool StartedRecently()
        {
            if (!s_startedAt.TryGetValue(s_shareId, out float at) || Time.realtimeSinceStartup - at > StartMemorySeconds)
                return false;
            Plugin.Log.LogInfo($"ServerWake : démarrage déjà demandé il y a {(int)(Time.realtimeSinceStartup - at)} s, pas de nouvelle demande");
            return true;
        }

        /// <summary>POST start ; résultat dans <see cref="s_result"/> (429 : rien d'envoyé à Nodecraft, on réessaiera).</summary>
        private static IEnumerator RequestStart()
        {
            s_phase = "Serveur en hibernation : démarrage demandé…";
            yield return Fetch(() => NodecraftClient.Start(s_shareId));
            if (!s_result.Success)
                yield break;
            s_startedAt[s_shareId] = Time.realtimeSinceStartup;
            Plugin.Log.LogInfo($"ServerWake : démarrage demandé à Nodecraft après {Elapsed} s");
        }

        /// <summary>État lu : publié aux pastilles, favori et nom suivis, une ligne de journal par changement d'état.</summary>
        private static void Observe(NodecraftStatus status)
        {
            ServerWakeBadges.Remember(s_shareId, status);
            s_server = LinkedFavorite.Sync(s_server, status);
            s_name = MultiBackendMatchmaking.GetServerName(s_server);
            string state = status.State == WakeState.Error
                ? $"erreur ({status.Message})"
                : $"status={status.Status}, jit_status={status.JitStatus}";
            if (state == s_loggedState)
                return;
            s_loggedState = state;
            Plugin.Log.LogInfo($"ServerWake : état Nodecraft {state} après {Elapsed} s");
        }

        /// <summary>Appel réseau sur une tâche d'arrière-plan ; résultat dans <see cref="s_result"/>.</summary>
        private static IEnumerator Fetch(System.Func<NodecraftStatus> call)
        {
            Task<NodecraftStatus> task = Task.Run(call);
            do
                yield return null; // au moins une frame : une boucle sur des refus immédiats (pause) ne fige pas le jeu
            while (!task.IsCompleted);
            // Les appels NodecraftClient ne lèvent pas, mais une exception ici laisserait la fenêtre figée.
            s_result = task.IsFaulted ? NodecraftStatus.Failure(task.Exception.GetBaseException().Message) : task.Result;
        }

        /// <summary>Message d'abandon, ou null pour continuer d'attendre. Erreurs réseau tolérées une fois l'attente lancée.</summary>
        private static string FailureOf(NodecraftStatus status, bool waiting, int errors)
        {
            switch (status.State)
            {
                case WakeState.Locked:
                    return "Le lien de partage Nodecraft est protégé par un mot de passe : démarre le serveur depuis sa page.";
                case WakeState.Unavailable:
                    return "Le serveur est archivé : il ne peut plus être démarré depuis le lien de partage.";
                case WakeState.Error:
                    return waiting && errors < MaxConsecutiveErrors ? null : $"Nodecraft ne répond pas correctement ({status.Message}).";
                default:
                    return null;
            }
        }

        private static void Finish()
        {
            Plugin.Log.LogInfo($"ServerWake : serveur prêt après {(int)(Time.realtimeSinceStartup - s_startTime)} s, connexion");
            FejdStartup startup = s_startup;
            ServerJoinData server = s_server;
            Stop();
            Join(startup, server);
        }

        private static void Join(FejdStartup startup, ServerJoinData server)
        {
            if (startup == null)
                return;
            s_bypass = server;
            startup.SetServerToJoin(server);
            startup.JoinServer();
        }

        private static void Fail(string message)
        {
            Plugin.Log.LogWarning($"ServerWake : {message}");
            FejdStartup startup = s_startup;
            ServerJoinData server = s_server;
            string page = NodecraftLink.PageUrl(s_shareId);
            Stop();
            PopupBase choice = null;
            choice = ServerWakePopups.ShowChoice($"Réveil de {s_name}", message
                    + "\n\nRejoindre : tenter la connexion quand même.\nVoir la page : ouvrir la page Nodecraft du serveur.",
                "Rejoindre", () => { ServerWakePopups.Close(choice); Join(startup, server); },
                "Voir la page", () => { ServerWakePopups.Close(choice); Application.OpenURL(page); RestoreMenu(startup); });
            if (choice == null)
                RestoreMenu(startup);
        }

        private static void Cancel()
        {
            Plugin.Log.LogInfo("ServerWake : attente annulée");
            FejdStartup startup = s_startup;
            Stop();
            RestoreMenu(startup);
        }

        private static void Stop()
        {
            if (s_routine != null && s_startup != null)
                s_startup.StopCoroutine(s_routine);
            s_routine = null;
            ServerWakePopups.Close(s_popup);
            s_popup = null;
        }

        /// <summary>
        /// Venu d'OnCharacterStart (AutoJoin, Continuer, sélection du personnage avec serveur en file), l'écran des
        /// personnages est déjà masqué : sans ceci, l'annulation laisserait un menu vide.
        /// </summary>
        private static void RestoreMenu(FejdStartup startup)
        {
            if (startup == null || startup.m_mainMenu.activeSelf || startup.m_characterSelectScreen.activeSelf
                || startup.m_startGamePanel.activeSelf)
                return;
            startup.ShowCharacterSelection();
        }
    }
}
