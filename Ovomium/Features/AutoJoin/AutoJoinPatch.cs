using HarmonyLib;
using Ovomium.Features.PasswordReveal;

namespace Ovomium.Features.AutoJoin
{
    /// <summary>
    /// Connexion automatique au lancement (outil de développement). Une fois FejdStartup.Start passé (personnages
    /// chargés, GUI prête), on emprunte le chemin natif de « +connect » : ProceedJoinRequest(joinData) (vérifie le
    /// privilège multijoueur, coupe la cinématique d'intro, met le serveur en file d'attente et ouvre la sélection du
    /// personnage) puis OnCharacterStart() (ce que fait le bouton Démarrer : SelectCharacter + JoinServer).
    /// m_instantStart supprime le fondu de 1,5 s avant le chargement. Un seul essai par lancement : après une
    /// déconnexion, le menu revient vanilla. Si le serveur demande un mot de passe, celui mémorisé par PasswordReveal
    /// est soumis tout seul (Postfix après celui de PasswordReveal, qui préremplit le champ) ; sinon saisie manuelle.
    /// </summary>
    internal static class AutoJoinPatch
    {
        /// <summary>Garde « un seul essai » dans l'AppDomain : un statique serait remis à zéro au rechargement à chaud.</summary>
        private const string AttemptedKey = "Ovomium.AutoJoin.Attempted";
        private static bool s_passwordPending;

        [HarmonyPatch(typeof(FejdStartup), "Start", new System.Type[0])]
        private static class StartPatch
        {
            private static void Postfix(FejdStartup __instance)
            {
                System.AppDomain domain = System.AppDomain.CurrentDomain;
                if (domain.GetData(AttemptedKey) != null)
                {
                    s_passwordPending = false;
                    return;
                }
                domain.SetData(AttemptedKey, true);
                if (AutoJoinConfig.IsRequested())
                    TryJoin(__instance);
            }
        }

        private static void TryJoin(FejdStartup startup)
        {
            int profile = AutoJoinTargets.FindProfile(startup.m_profiles, AutoJoinConfig.Character.Value, startup.m_profileIndex);
            if (profile < 0)
            {
                Plugin.Log.LogInfo($"AutoJoin : personnage « {AutoJoinConfig.Character.Value} » introuvable, abandon");
                return;
            }
            ServerJoinData server = AutoJoinTargets.FindServer(AutoJoinConfig.Server.Value);
            if (!server.IsValid)
            {
                Plugin.Log.LogInfo($"AutoJoin : serveur « {AutoJoinConfig.Server.Value} » inconnu (aucun serveur récent ?), abandon");
                return;
            }
            Plugin.Log.LogInfo($"AutoJoin : personnage {startup.m_profiles[profile].GetName()} "
                + $"({startup.m_profiles[profile].GetFilename()}), serveur {MultiBackendMatchmaking.GetServerName(server)} ({server})");

            startup.m_profileIndex = profile;
            startup.m_instantStart = true;
            startup.ProceedJoinRequest(server);
            if (!startup.m_queuedJoinServer.IsValid)
            {
                Plugin.Log.LogInfo("AutoJoin : demande de connexion refusée par le jeu (privilège multijoueur ?)");
                return;
            }
            s_passwordPending = true;
            startup.OnCharacterStart();
            Plugin.Log.LogInfo("AutoJoin : connexion lancée");
        }

        [HarmonyPatch(typeof(ZNet), "RPC_ClientHandshake", typeof(ZRpc), typeof(bool), typeof(string))]
        [HarmonyPriority(Priority.Low)]
        private static class ClientHandshakePatch
        {
            private static void Postfix(ZNet __instance, bool needPassword)
            {
                if (!s_passwordPending)
                    return;
                s_passwordPending = false;
                if (!needPassword || __instance.m_passwordDialog == null || !__instance.m_passwordDialog.gameObject.activeSelf)
                    return;
                string password = PasswordStore.Get(ZNet.GetServerString(true));
                if (password == null)
                {
                    Plugin.Log.LogInfo("AutoJoin : mot de passe non mémorisé pour ce serveur, saisie manuelle");
                    return;
                }
                Plugin.Log.LogInfo("AutoJoin : mot de passe mémorisé soumis");
                __instance.OnPasswordEntered(password);
            }
        }
    }
}
