using BepInEx.Configuration;

namespace OvoMiam.Features.MinimapSize
{
    /// <summary>Réglages de la fonctionnalité « taille de la minicarte ».</summary>
    internal static class MinimapSizeConfig
    {
        public const float MinScale = 0.5f;
        public const float MaxScale = 3f;

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> Scale { get; private set; }
        public static ConfigEntry<float> Step { get; private set; }
        public static ConfigEntry<float> MinZoom { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("MinimapSize", "Enabled", true,
                "Shift + touches de zoom de la carte (pavé num + / - par défaut) agrandit / réduit la minicarte du HUD "
                + "(maintenir la touche répète), à densité terrain / pixel constante, icônes à taille constante ; les indicateurs d'état sont poussés à gauche.");
            Scale = config.Bind("MinimapSize", "Scale", 1f,
                new ConfigDescription("Facteur d'échelle de la minicarte, modifié en jeu par Shift + touches de zoom de la carte.",
                    new AcceptableValueRange<float>(MinScale, MaxScale)));
            Step = config.Bind("MinimapSize", "Step", 0.1f,
                "Variation du facteur d'échelle à chaque pas (appui, ou répétition en maintenant la touche).");
            MinZoom = config.Bind("MinimapSize", "MinZoom", 0.0025f,
                new ConfigDescription("Zoom avant maximal de la minicarte (fraction de la carte affichée, à l'échelle 1). "
                    + "Vanilla : 0.01, qui est aussi le zoom de départ ; chaque pas de zoom divise par 2 (0.0025 = 2 pas de plus).",
                    new AcceptableValueRange<float>(0.001f, 0.01f)));
        }
    }
}
