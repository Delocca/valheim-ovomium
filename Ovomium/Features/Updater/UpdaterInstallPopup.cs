using UnityEngine;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Fenêtre de l'installation au menu (<see cref="HotInstall"/>) : changelog et ligne de progression animée, sans
    /// bouton (<c>TaskPopup</c> vanilla, corps réécrit chaque frame), puis résultat avec OK (<c>WarningPopup</c>).
    /// </summary>
    internal static class UpdaterInstallPopup
    {
        private const string Highlight = "#E8D5A8";
        private static TaskPopup s_task;

        /// <summary>Affiche (ou réaffiche, après un rechargement ou une fenêtre vanilla) la progression.</summary>
        public static void UpdateProgress(bool installing)
        {
            string text = Body(Status(installing));
            if (UpdaterPopupHost.IsShown(s_task))
            {
                UpdaterPopupHost.SetBody(text);
                return;
            }
            if (!UnifiedPopup.IsAvailable() || UnifiedPopup.IsVisible())
                return;
            s_task = new TaskPopup($"Ovomium {UpdateState.Version}", text, localizeText: false);
            UpdaterPopupHost.Show(s_task);
        }

        public static void ShowResult(bool ok, string message)
        {
            s_task = null;
            if (!UnifiedPopup.IsAvailable())
                return;
            string header = ok ? $"Ovomium {UpdateState.Version} installée" : $"Ovomium {UpdateState.Version}";
            string text = ok ? Body($"<color={Highlight}>{message}</color>") : message;
            UpdaterPopupHost.Show(new WarningPopup(header, text, UpdaterPopupHost.Close, localizeText: false));
        }

        private static string Body(string status)
        {
            string changelog = ChangelogFormatter.ToRichText(UpdateState.Changelog);
            return (changelog.Length > 0 ? changelog + "\n\n" : "") + status;
        }

        /// <summary>Points animés, et pourcentage (ou Mo reçus si la taille est inconnue) pendant le téléchargement.</summary>
        private static string Status(bool installing)
        {
            string dots = new string('.', 1 + (int)(Time.unscaledTime * 2f) % 3);
            if (installing)
                return $"<color={Highlight}>Installation{dots}</color>";
            long done = UpdateState.DownloadedBytes, size = UpdateState.DownloadSize;
            string amount = size > 0 ? $"{Mathf.Clamp(done * 100f / size, 0f, 100f):0} %" : $"{done / 1048576f:0.0} Mo";
            return $"<color={Highlight}>Installation en cours{dots}</color>  {amount}";
        }
    }
}
