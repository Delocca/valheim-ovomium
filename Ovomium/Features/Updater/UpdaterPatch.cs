using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Mise à jour du mod depuis le jeu : vérification au menu principal, ligne sous la version vanilla, puis
    /// - au menu : installation sans question ni relance (<see cref="HotInstall"/>) ;
    /// - en partie (menu sauté par AutoJoin) : fenêtre oui/non au premier menu Échap (<see cref="UpdaterPopup"/>),
    ///   téléchargement dans <c>update/</c> et relance du jeu (<see cref="GameRelauncher"/>).
    /// Le patcher Ovomium.Updater installe <c>update/</c> au lancement suivant dans les deux cas.
    /// Les threads de fond n'écrivent que <see cref="UpdateState"/> ; le guetteur applique l'état à l'UI sur le
    /// thread principal.
    /// </summary>
    internal static class UpdaterPatch
    {
        /// <summary>Pose le guetteur sur l'objet hôte du cœur (détruit par le chargeur au rechargement).</summary>
        public static void Install(GameObject host)
        {
            host.AddComponent<UpdateWatcher>();
            UpdaterConsole.Install();
        }

        private static bool s_afterLoadPending;

        /// <summary>
        /// Fin du chargement du cœur : résultat d'une installation à chaud, lu à la première frame du guetteur. Le
        /// chargeur ne publie le chemin du cœur démarré (<c>Ovomium.Loader.CorePath</c>) qu'au retour de
        /// <c>Plugin.Load</c> : lu ici même, il désignait encore l'ancien cœur et une installation réussie
        /// s'annonçait en échec (1.2.0 → 1.3.0).
        /// </summary>
        public static void AfterLoad()
        {
            s_afterLoadPending = true;
        }

        /// <summary>Notre fenêtre est retirée ; la version suivante la réaffiche d'après l'état (oui/non reproposée).</summary>
        internal static void Unload()
        {
            UpdaterMenuLabel.Unload();
            UpdaterPopupHost.Close();
            if (UpdateState.PopupShown && !UpdateState.PopupDone)
                UpdateState.PopupShown = false;
            UpdaterConsole.Unload();
        }

        [HarmonyPatch(typeof(FejdStartup), "Start", new System.Type[0])]
        private static class StartPatch
        {
            private static void Postfix()
            {
                UpdateChecker.StartIfNeeded();
            }
        }

        /// <summary>Cas AutoJoin : le menu principal a été sauté, la fenêtre attend le premier menu Échap.</summary>
        [HarmonyPatch(typeof(Menu), "Show", new System.Type[0])]
        private static class MenuShowPatch
        {
            private static void Postfix()
            {
                if (UpdaterConfig.Enabled.Value)
                    UpdaterPopup.TryShow();
            }
        }

        /// <summary>Applique l'état de la mise à jour à l'interface, une fois par changement.</summary>
        private sealed class UpdateWatcher : MonoBehaviour
        {
            private string m_labelText = "";

            private void Update()
            {
                if (s_afterLoadPending)
                {
                    s_afterLoadPending = false;
                    if (UpdaterConfig.Enabled.Value) HotInstall.AfterLoad();
                }
                if (!UpdaterConfig.Enabled.Value || !(UpdateState.Started || UpdateState.HotInstall))
                    return;
                // Fenêtre emportée par un changement de scène sans réponse (AutoJoin) : à reproposer au menu Échap.
                if (UpdateState.PopupShown && !UpdateState.PopupDone && !UnifiedPopup.IsVisible())
                {
                    UpdateState.PopupShown = false;
                    UpdaterPopupLayout.Restore();
                }
                if (UpdateState.Downloaded && UpdateState.RelaunchRequested)
                    Relaunch();
                if (Player.m_localPlayer == null && FejdStartup.instance != null)
                    UpdateMainMenu(FejdStartup.instance);
                else if (Player.m_localPlayer != null && MessageHud.instance != null)
                    UpdateInGame();
            }

            /// <summary>Une seule tentative ; si le script ne part pas, l'installation attend le prochain lancement
            /// (messages habituels).</summary>
            private static void Relaunch()
            {
                UpdateState.RelaunchRequested = false;
                Plugin.Log.LogInfo($"Updater : version {UpdateState.Version} téléchargée, relance du jeu");
                if (GameRelauncher.Start())
                    Application.Quit();
            }

            private void UpdateMainMenu(FejdStartup startup)
            {
                string text = UpdaterMenuLabel.CurrentText();
                if (text != m_labelText)
                {
                    m_labelText = text;
                    UpdaterMenuLabel.Apply(startup, text);
                }
                HotInstall.Tick(startup);
            }

            /// <summary>Messages en haut à gauche : « disponible » (menu sauté par AutoJoin) puis « téléchargée ».</summary>
            private static void UpdateInGame()
            {
                if (UpdateState.Available && !UpdateState.PopupDone && !UpdateState.HudMessageDone)
                {
                    UpdateState.HudMessageDone = true;
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft,
                        $"Ovomium {UpdateState.Version} disponible : voir le menu Échap");
                }
                if (UpdateState.Downloaded && !UpdateState.HudDownloadedDone)
                {
                    UpdateState.HudDownloadedDone = true;
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft,
                        $"Ovomium {UpdateState.Version} téléchargée : installée au prochain lancement");
                }
            }
        }
    }
}
