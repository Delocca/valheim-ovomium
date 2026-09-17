using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.StartupSkip
{
    /// <summary>Réglages de la fonctionnalité « démarrage rapide ».</summary>
    internal static class StartupSkipConfig
    {
        public static ConfigEntry<bool> SkipLogos { get; private set; }

        public static void Bind(ConfigFile config)
        {
            SkipLogos = config.Bind("StartupSkip", "SkipLogos", true,
                new ConfigDescription("Sauter les logos Coffee Stain et Iron Gate au lancement du jeu (2 s chacun).",
                    null, new SettingLabel("Sauter les logos", restartRequired: true)));
        }
    }
}
