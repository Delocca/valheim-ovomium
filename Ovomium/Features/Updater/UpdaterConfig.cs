using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.Updater
{
    /// <summary>Réglages de la fonctionnalité « mise à jour du mod depuis le jeu ».</summary>
    internal static class UpdaterConfig
    {
        // Doit rester identique à tools/release.sh et installer/Installer-Ovomium.bat (nom du dépôt).
        private const string DefaultReleasesApiUrl = "https://api.github.com/repos/Delocca/valheim-ovomium/releases/latest";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<string> ReleasesApiUrl { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("Updater", "Enabled", true,
                new ConfigDescription("Au lancement, vérifie si une nouvelle version d'Ovomium est publiée et propose de la "
                    + "télécharger (changelog affiché) ; elle s'installe au lancement suivant du jeu.",
                    null, new SettingLabel("Activé")));
            ReleasesApiUrl = config.Bind("Updater", "ReleasesApiUrl", DefaultReleasesApiUrl,
                "URL de l'API GitHub « dernière release » interrogée (vide = pas de vérification).");
        }
    }
}
