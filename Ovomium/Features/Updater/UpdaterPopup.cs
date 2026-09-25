namespace Ovomium.Features.Updater
{
    /// <summary>
    /// En partie seulement (menu principal sauté par AutoJoin ; au menu, c'est <see cref="HotInstall"/>) : fenêtre
    /// vanilla oui/non « Ovomium x.y.z » avec le changelog au premier menu Échap ; oui télécharge puis relance le jeu
    /// (<see cref="GameRelauncher"/>, la sortie sauvegarde), non repousse à la prochaine session. Les callbacks de
    /// <c>YesNoPopup</c> ne ferment pas la fenêtre : fermeture explicite.
    /// </summary>
    internal static class UpdaterPopup
    {
        /// <summary>Affiche la fenêtre si elle n'est ni en attente de réponse ni déjà répondue ; vrai si affichée.</summary>
        public static bool TryShow()
        {
            if (UpdateState.PopupShown || UpdateState.PopupDone || !UpdateState.Available || UpdateState.Downloaded
                || UpdateState.Downloading)
                return false;
            if (!UnifiedPopup.IsAvailable() || UnifiedPopup.IsVisible())
                return false;
            UpdateState.PopupShown = true;
            string header = $"Ovomium {UpdateState.Version}";
            string body = ChangelogFormatter.ToRichText(UpdateState.Changelog);
            string text = (body.Length > 0 ? body + "\n\n" : "") + "Télécharger et relancer le jeu maintenant ?";
            UpdaterPopupHost.Show(new YesNoPopup(header, text, OnYes, OnNo, localizeText: false));
            Plugin.Log.LogInfo($"Updater : fenêtre de mise à jour {UpdateState.Version} affichée");
            return true;
        }

        private static void OnYes()
        {
            UpdateState.PopupDone = true;
            UpdaterPopupHost.Close();
            UpdateState.RelaunchRequested = true;
            UpdateDownloader.Start();
            if (MessageHud.instance != null)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft,
                    $"Ovomium {UpdateState.Version} : téléchargement… le jeu va se relancer");
        }

        private static void OnNo()
        {
            UpdateState.PopupDone = true;
            UpdaterPopupHost.Close();
            Plugin.Log.LogInfo("Updater : mise à jour refusée pour cette session");
        }
    }
}
