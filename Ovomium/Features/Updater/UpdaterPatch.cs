using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Mise à jour du mod depuis le jeu : vérification au menu principal, ligne sous la version vanilla, fenêtre
    /// oui/non avec le changelog (au menu principal, ou au premier menu Échap si le menu a été sauté par AutoJoin),
    /// téléchargement dans <c>update/</c>, installation par le patcher Ovomium.Updater au lancement suivant.
    /// Les threads de fond n'écrivent que <see cref="UpdateState"/> ; le guetteur applique l'état à l'UI sur le
    /// thread principal.
    /// </summary>
    internal static class UpdaterPatch
    {
        /// <summary>Pose le guetteur sur l'objet du plugin (détruit avec lui au rechargement à chaud).</summary>
        public static void Install(GameObject host)
        {
            if (host.GetComponent<UpdateWatcher>() == null)
                host.AddComponent<UpdateWatcher>();
        }

        internal static void Unload()
        {
            UpdaterMenuLabel.Unload();
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
                if (!UpdaterConfig.Enabled.Value || !UpdateState.Started)
                    return;
                // Fenêtre emportée par un changement de scène sans réponse (AutoJoin) : à reproposer au menu Échap.
                if (UpdateState.PopupShown && !UpdateState.PopupDone && !UnifiedPopup.IsVisible())
                    UpdateState.PopupShown = false;
                if (Player.m_localPlayer == null && FejdStartup.instance != null)
                    UpdateMainMenu(FejdStartup.instance);
                else if (Player.m_localPlayer != null && MessageHud.instance != null)
                    UpdateInGame();
            }

            private void UpdateMainMenu(FejdStartup startup)
            {
                string text = UpdaterMenuLabel.CurrentText();
                if (text != m_labelText)
                {
                    m_labelText = text;
                    UpdaterMenuLabel.Apply(startup, text);
                }
                // Seulement sur un écran de menu : pendant une jonction (AutoJoin, ContinueButton) tous sont masqués
                // et la scène va disparaître ; l'écran Loading, lui, n'est actif qu'au chargement effectif.
                if (MenuScreenVisible(startup))
                    UpdaterPopup.TryShow();
            }

            private static bool MenuScreenVisible(FejdStartup startup)
            {
                return Active(startup.m_mainMenu) || Active(startup.m_characterSelectScreen) || Active(startup.m_startGamePanel);
            }

            private static bool Active(GameObject screen)
            {
                return screen != null && screen.activeInHierarchy;
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
