using System.IO;
using System.Text;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Commande console <c>ovomium_updatetest [archive.zip]</c>, au menu : rejoue le flux complet d'installation à
    /// chaud (<see cref="HotInstall"/>) avec une archive locale au format publié (<c>tools/package.sh</c>), par défaut
    /// <c>BepInEx/plugins/Ovomium/test-update.zip</c>, et un changelog factice de longueur maximale (mise en page).
    /// Le dossier <c>update/</c> déposé est installé par le patcher au lancement suivant, comme une vraie mise à jour.
    /// Enregistrée dans <c>Terminal.commands</c> (statique, publicisé) au chargement du cœur, retirée au déchargement.
    /// </summary>
    internal static class UpdaterConsole
    {
        private const string Command = "ovomium_updatetest";
        private const string DefaultArchive = "test-update.zip";

        public static void Install()
        {
            _ = new Terminal.ConsoleCommand(Command,
                "[archive.zip] Ovomium : installation à chaud d'une archive locale (défaut : plugins/Ovomium/test-update.zip)",
                new Terminal.ConsoleEvent(Run));
        }

        public static void Unload()
        {
            Terminal.commands.Remove(Command);
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            string archive = args.Length > 1 ? args.ArgsAll.Trim() : Path.Combine(UpdateChecker.PluginFolder, DefaultArchive);
            string result = Prepare(archive);
            args.Context?.AddString(result);
            Plugin.Log.LogInfo(result);
        }

        private static string Prepare(string archive)
        {
            if (Player.m_localPlayer != null || FejdStartup.instance == null)
                return "Ovomium : à lancer au menu principal";
            if (!File.Exists(archive))
                return $"Ovomium : archive introuvable : {archive}";
            if (UpdateState.Downloading || UpdateState.HotInstall)
                return "Ovomium : une installation est déjà en cours";
            UpdateState.Started = true;
            UpdateState.Available = true;
            UpdateState.Downloaded = false;
            UpdateState.PopupShown = false;
            UpdateState.PopupDone = false;
            UpdateState.Version = "test";  // remplacée par le version.txt de l'archive
            UpdateState.Changelog = FakeChangelog();
            UpdateState.DownloadUrl = new System.Uri(Path.GetFullPath(archive)).AbsoluteUri;
            UpdateState.DownloadSize = 0;
            return $"Ovomium : installation de test depuis {archive} (fermer la console)";
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
