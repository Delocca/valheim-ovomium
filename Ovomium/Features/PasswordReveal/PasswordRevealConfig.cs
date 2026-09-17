using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.PasswordReveal
{
    /// <summary>Réglages de la fonctionnalité « afficher le mot de passe du serveur ».</summary>
    internal static class PasswordRevealConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> ShowPassword { get; private set; }
        public static ConfigEntry<string> ShowLabel { get; private set; }
        public static ConfigEntry<string> HideLabel { get; private set; }
        public static ConfigEntry<string> RememberLabel { get; private set; }
        public static ConfigEntry<string> OkLabel { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("PasswordReveal", "Enabled", true,
                new ConfigDescription("Bouton à droite du champ « mot de passe du serveur » (connexion à un serveur) pour afficher ou masquer "
                    + "le mot de passe saisi, case « Mémoriser » sous le champ pour le retrouver prérempli à la prochaine "
                    + "connexion à ce serveur (stocké obfusqué dans config/ovo.ovomium.passwords.txt), et bouton « OK » "
                    + "pour valider à la souris.",
                    null, new SettingLabel("Activé")));
            ShowPassword = config.Bind("PasswordReveal", "ShowPassword", false,
                new ConfigDescription("Mot de passe affiché en clair (mémorisé d'une connexion à l'autre ; le bouton bascule cette valeur).",
                    null, new SettingLabel("Mot de passe en clair")));
            ShowLabel = config.Bind("PasswordReveal", "ShowLabel", "Afficher",
                "Libellé du bouton quand le mot de passe est masqué.");
            HideLabel = config.Bind("PasswordReveal", "HideLabel", "Masquer",
                "Libellé du bouton quand le mot de passe est affiché.");
            RememberLabel = config.Bind("PasswordReveal", "RememberLabel", "Mémoriser",
                "Libellé de la case à cocher « mémoriser le mot de passe pour ce serveur ».");
            OkLabel = config.Bind("PasswordReveal", "OkLabel", "OK",
                "Libellé du bouton de validation (même effet que la touche Entrée).");
        }
    }
}
