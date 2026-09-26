using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.UtilitySlots
{
    /// <summary>Réglages de la fonctionnalité « plusieurs objets utilitaires portés ensemble ».</summary>
    internal static class UtilitySlotsConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<int> MaxItems { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("UtilitySlots", "Enabled", true,
                new ConfigDescription("Permet de porter plusieurs objets utilitaires à la fois (Megingjord, Bréchet, Lumière de feu-follet).",
                    null, new SettingLabel("Activé")));
            MaxItems = config.Bind("UtilitySlots", "MaxItems", 3,
                new ConfigDescription("Nombre d'objets utilitaires portés ensemble (vanilla : 1). Au-delà, équiper un objet retire celui équipé depuis le plus longtemps.",
                    new AcceptableValueRange<int>(1, 3), new SettingLabel("Objets utilitaires portés")));
        }
    }
}
