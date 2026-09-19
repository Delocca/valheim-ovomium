using TMPro;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Fenêtre vanilla oui/non « Ovomium x.y.z » avec le changelog : oui télécharge, non repousse à la prochaine
    /// session. Les callbacks de <c>YesNoPopup</c> ne ferment pas la fenêtre : <c>Pop()</c> explicite. Le corps est
    /// centré par <c>ResetUI</c> à chaque affichage : on l'aligne à gauche après le Push, rien à restaurer.
    /// </summary>
    internal static class UpdaterPopup
    {
        /// <summary>Affiche la fenêtre si elle n'est ni en attente de réponse ni déjà répondue ; vrai si affichée.</summary>
        public static bool TryShow()
        {
            if (UpdateState.PopupShown || UpdateState.PopupDone || !UpdateState.Available || UpdateState.Downloaded)
                return false;
            if (!UnifiedPopup.IsAvailable() || UnifiedPopup.IsVisible())
                return false;
            UpdateState.PopupShown = true;
            string header = $"Ovomium {UpdateState.Version}";
            string body = ChangelogFormatter.ToRichText(UpdateState.Changelog);
            string text = (body.Length > 0 ? body + "\n\n" : "") + "Télécharger maintenant ? La mise à jour s'installera au prochain lancement du jeu.";
            UnifiedPopup.Push(new YesNoPopup(header, text, OnYes, OnNo, localizeText: false));
            AlignBodyLeft();
            Plugin.Log.LogInfo($"Updater : fenêtre de mise à jour {UpdateState.Version} affichée");
            return true;
        }

        private static void OnYes()
        {
            UpdateState.PopupDone = true;
            UnifiedPopup.Pop();
            UpdateDownloader.Start();
        }

        private static void OnNo()
        {
            UpdateState.PopupDone = true;
            UnifiedPopup.Pop();
            Plugin.Log.LogInfo("Updater : mise à jour refusée pour cette session");
        }

        private static void AlignBodyLeft()
        {
            TextMeshProUGUI body = UnifiedPopup.instance != null ? UnifiedPopup.instance.bodyText : null;
            if (body != null)
                body.alignment = TextAlignmentOptions.TopLeft;
        }
    }
}
