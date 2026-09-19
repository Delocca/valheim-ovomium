using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.FocusClick
{
    /// <summary>Réglages de la fonctionnalité « clic de reprise du focus ignoré ».</summary>
    internal static class FocusClickConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> IgnoreSeconds { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("FocusClick", "Enabled", true,
                new ConfigDescription("Quand on clique sur la fenêtre du jeu pour lui redonner le focus, ce clic ne déclenche ni "
                    + "attaque, ni blocage, ni interaction, ni pose de construction.",
                    null, new SettingLabel("Activé")));
            IgnoreSeconds = config.Bind("FocusClick", "IgnoreSeconds", 0.3f,
                new ConfigDescription("Durée pendant laquelle les clics sont ignorés après le retour du focus, en secondes.",
                    new AcceptableValueRange<float>(0.1f, 1f), new SettingLabel("Durée d'ignorance (s)")));
        }
    }
}
