using System.Text;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Commande console <c>ovomium_updatepopup</c> : affiche la fenêtre de mise à jour avec une version factice
    /// (test de mise en page), « oui » ne télécharge rien (<see cref="UpdateState.FakePopup"/>). Enregistrée dans
    /// <c>Terminal.commands</c> (statique, publicisé) au chargement du plugin, retirée au déchargement.
    /// </summary>
    internal static class UpdaterConsole
    {
        private const string Command = "ovomium_updatepopup";
        private const string FakeVersion = "9.9.9";

        public static void Install()
        {
            _ = new Terminal.ConsoleCommand(Command,
                "Ovomium : affiche la fenêtre de mise à jour avec une version factice (« oui » ne télécharge rien)",
                new Terminal.ConsoleEvent(Run));
        }

        public static void Unload()
        {
            Terminal.commands.Remove(Command);
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            UpdateState.FakePopup = true;
            UpdateState.PopupShown = false;
            UpdateState.PopupDone = false;
            UpdateState.Available = true;
            UpdateState.Version = FakeVersion;
            if (UpdateState.Changelog.Trim().Length == 0)
                UpdateState.Changelog = FakeChangelog();
            bool shown = UpdaterPopup.TryShow();
            string result = shown ? "Ovomium : fenêtre de mise à jour de test affichée"
                : UpdateState.Downloaded ? "Ovomium : fenêtre non affichée, une mise à jour est déjà téléchargée"
                : "Ovomium : fenêtre non affichée (UnifiedPopup absent ou déjà visible)";
            args.Context?.AddString(result);
            Plugin.Log.LogInfo(result);
        }

        /// <summary>Douze lignes longues, la limite de <see cref="ChangelogFormatter.MaxLines"/>.</summary>
        private static string FakeChangelog()
        {
            var sb = new StringBuilder();
            for (int i = 1; i <= ChangelogFormatter.MaxLines; i++)
                sb.Append($"- **Fonctionnalité de test {i}** : une phrase assez longue pour dépasser la largeur de la "
                    + $"fenêtre et vérifier le retour à la ligne, la taille du texte et la hauteur du panneau (Section{i}).\n");
            return sb.ToString();
        }
    }
}
