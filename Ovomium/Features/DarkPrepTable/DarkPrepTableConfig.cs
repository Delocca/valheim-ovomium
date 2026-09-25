using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.DarkPrepTable
{
    /// <summary>Réglages de la fonctionnalité « table de préparation en bois sombre ».</summary>
    internal static class DarkPrepTableConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> Brightness { get; private set; }
        public static ConfigEntry<float> Saturation { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("DarkPrepTable", "Enabled", true,
                new ConfigDescription("Table de préparation culinaire en bois sombre : même texture, assombrie (visible chez soi "
                    + "seulement).",
                    null, new SettingLabel("Activé")));
            Brightness = config.Bind("DarkPrepTable", "Brightness", 0.25f,
                new ConfigDescription("Luminosité du bois de la table de préparation (1 : jeu inchangé, 0.25 par défaut).",
                    new AcceptableValueRange<float>(0.05f, 1f), new SettingLabel("Luminosité du bois", livePreview: true)));
            Saturation = config.Bind("DarkPrepTable", "Saturation", 1.3f,
                new ConfigDescription("Saturation du bois de la table de préparation (1 : jeu inchangé, 1.3 par défaut).",
                    new AcceptableValueRange<float>(0.5f, 2f), new SettingLabel("Saturation du bois", livePreview: true)));
        }
    }
}
