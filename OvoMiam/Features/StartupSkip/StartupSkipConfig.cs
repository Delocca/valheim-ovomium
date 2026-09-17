using BepInEx.Configuration;

namespace OvoMiam.Features.StartupSkip
{
    /// <summary>Réglages de la fonctionnalité « démarrage rapide ».</summary>
    internal static class StartupSkipConfig
    {
        public static ConfigEntry<bool> SkipLogos { get; private set; }
        public static ConfigEntry<bool> IntroVideo { get; private set; }

        public static void Bind(ConfigFile config)
        {
            SkipLogos = config.Bind("StartupSkip", "SkipLogos", true,
                "Sauter les logos Coffee Stain et Iron Gate au lancement du jeu (2 s chacun).");
            IntroVideo = config.Bind("StartupSkip", "IntroVideo", true,
                "Jouer la vidéo d'introduction avant le menu principal (non : arriver directement au menu).");
        }
    }
}
