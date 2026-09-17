using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.MenuDoubleClick
{
    /// <summary>Réglages de la fonctionnalité « double-clic dans les listes du menu principal ».</summary>
    internal static class MenuDoubleClickConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> DoubleClickSeconds { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("MenuDoubleClick", "Enabled", true,
                new ConfigDescription("Menu principal : un double-clic sur un monde le démarre, un double-clic sur un serveur s'y connecte "
                    + "(même effet que le bouton Démarrer / Connecter, seulement si ce bouton est actif).",
                    null, new SettingLabel("Activé")));
            DoubleClickSeconds = config.Bind("MenuDoubleClick", "DoubleClickSeconds", 0.4f,
                new ConfigDescription("Délai maximal entre les deux clics d'un double-clic, en secondes.",
                    new AcceptableValueRange<float>(0.1f, 1f), new SettingLabel("Délai du double-clic (s)")));
        }
    }
}
