using System.IO;
using Ovomium.Features.ServerWake;
using UnityEngine;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Mise à jour sans relance, au menu seulement (menu principal ou sélection du personnage, au repos) : dès qu'une
    /// version est disponible, sans question, fenêtre de progression (<see cref="UpdaterInstallPopup"/>),
    /// téléchargement dans <c>update/</c>, puis le chargeur remplace ce cœur par <c>update/Ovomium.Core.dll</c>
    /// (<see cref="Plugin.RequestReload"/>, à la frame suivante). Le cœur démarré ensuite, nouveau ou ancien redémarré
    /// après un échec, affiche le résultat (<see cref="AfterLoad"/>). Les fichiers en place ne sont pas touchés : le
    /// patcher installe <c>update/</c> au lancement suivant. Ce qui tourne avant le menu (logos, AutoJoin, écran de
    /// démarrage) ne profite de la nouvelle version qu'à ce lancement.
    /// </summary>
    internal static class HotInstall
    {
        /// <summary>Clés AppDomain posées par le chargeur (Ovomium.Loader.OvomiumLoader).</summary>
        private const string LoaderCorePathKey = "Ovomium.Loader.CorePath";
        private const string LoaderLastErrorKey = "Ovomium.Loader.LastError";

        /// <summary>Chaque frame au menu (FejdStartup présent, pas de joueur).</summary>
        public static void Tick(FejdStartup startup)
        {
            if (UpdateState.HotInstall)
                Progress();
            else if (CanBegin(startup))
                Begin();
        }

        private static bool CanBegin(FejdStartup startup)
        {
            return UpdateState.Available && !UpdateState.PopupDone && !UpdateState.Downloading && !UpdateState.Downloaded
                && Plugin.RequestReload != null && Idle(startup);
        }

        /// <summary>Menu au repos : aucun chargement, fenêtre, Paramètres ni réveil de serveur en cours.</summary>
        private static bool Idle(FejdStartup startup)
        {
            return (Active(startup.m_mainMenu) || Active(startup.m_characterSelectScreen)) && !Active(startup.m_loading)
                && !UnifiedPopup.IsVisible() && Settings.instance == null && !ServerWakeSession.IsRunning;
        }

        private static bool Active(GameObject screen)
        {
            return screen != null && screen.activeInHierarchy;
        }

        private static void Begin()
        {
            UpdateState.HotInstall = true;
            UpdateState.PopupDone = true;  // plus de fenêtre oui/non cette session
            Plugin.Log.LogInfo($"Updater : installation de la version {UpdateState.Version} au menu");
            UpdateDownloader.Start();
            UpdaterInstallPopup.UpdateProgress(false);
        }

        private static void Progress()
        {
            if (UpdateState.HotReloadRequested)
                return;  // le chargeur agit à la frame suivante
            if (UpdateState.Downloading)
            {
                UpdaterInstallPopup.UpdateProgress(false);
                return;
            }
            if (!UpdateState.Downloaded)
            {
                Finish(false, $"Téléchargement impossible : {UpdateState.Error}\n\nOvomium {PluginVersion.Value} reste active, "
                    + "nouvel essai au prochain lancement du jeu.");
                return;
            }
            UpdaterInstallPopup.UpdateProgress(true);
            UpdateState.HotReloadRequested = true;
            Plugin.Log.LogInfo($"Updater : démarrage à chaud de {UpdateDownloader.UpdateCorePath}");
            Plugin.RequestReload(UpdateDownloader.UpdateCorePath);
        }

        /// <summary>Appelé à la fin de <see cref="Plugin.Load"/> : résultat d'une installation à chaud demandée.</summary>
        public static void AfterLoad()
        {
            if (!UpdateState.HotReloadRequested)
                return;
            UpdateState.HotReloadRequested = false;
            if (Loaded(UpdateDownloader.UpdateCorePath))
            {
                Plugin.Log.LogInfo($"Updater : version {PluginVersion.Value} installée sans relance");
                Finish(true, "Mise à jour installée, bon jeu !");
                return;
            }
            string error = System.AppDomain.CurrentDomain.GetData(LoaderLastErrorKey) as string ?? "";
            Finish(false, $"La nouvelle version n'a pas pu démarrer ({FirstLine(error)}).\n\nOvomium {PluginVersion.Value} "
                + $"reste active, la version {UpdateState.Version} sera installée au prochain lancement du jeu.");
        }

        private static bool Loaded(string path)
        {
            string loaded = System.AppDomain.CurrentDomain.GetData(LoaderCorePathKey) as string;
            return loaded != null && Path.GetFullPath(loaded) == Path.GetFullPath(path);
        }

        /// <summary>Succès : plus rien en attente (libellé effacé, revérification au prochain retour au menu).</summary>
        private static void Finish(bool ok, string message)
        {
            UpdateState.HotInstall = false;
            if (ok)
            {
                UpdateState.Available = false;
                UpdateState.Downloaded = false;
                UpdateState.Started = false;
            }
            UpdaterInstallPopup.ShowResult(ok, message);
        }

        private static string FirstLine(string text)
        {
            int end = text.IndexOfAny(new[] { '\r', '\n' });
            string line = (end < 0 ? text : text.Substring(0, end)).Trim();
            return line.Length > 0 ? line : "erreur inconnue";
        }
    }
}
