using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.AmbientOcclusion
{
    /// <summary>Réglages de la fonctionnalité « occlusion ambiante ».</summary>
    internal static class AmbientOcclusionConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> Intensity { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("AmbientOcclusion", "Enabled", true,
                new ConfigDescription("Intensité de l'occlusion ambiante (SSAO) réglable, par rapport à celle que le jeu choisit selon "
                    + "l'environnement et l'heure.",
                    null, new SettingLabel("Activé")));
            Intensity = config.Bind("AmbientOcclusion", "Intensity", 0.8f,
                new ConfigDescription("Multiplicateur de l'intensité vanilla (0 : pas d'occlusion, 1 : jeu inchangé, 0.8 par défaut).",
                    new AcceptableValueRange<float>(0f, 1f), new SettingLabel("Intensité (× vanilla)")));
        }
    }
}
