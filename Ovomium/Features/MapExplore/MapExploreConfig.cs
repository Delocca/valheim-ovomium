using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.MapExplore
{
    /// <summary>Réglages de la fonctionnalité « rayon de découverte de la carte ».</summary>
    internal static class MapExploreConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> LandFactor { get; private set; }
        public static ConfigEntry<float> BoatFactor { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("MapExplore", "Enabled", true,
                new ConfigDescription("Multiplie le rayon de découverte de la carte autour du joueur (vanilla : 100 m).",
                    null, new SettingLabel("Activé")));
            LandFactor = config.Bind("MapExplore", "LandFactor", 3f,
                new ConfigDescription("Facteur du rayon de découverte à pied.",
                    new AcceptableValueRange<float>(1f, 10f), new SettingLabel("Facteur à pied")));
            BoatFactor = config.Bind("MapExplore", "BoatFactor", 5f,
                new ConfigDescription("Facteur du rayon de découverte à bord d'un bateau.",
                    new AcceptableValueRange<float>(1f, 10f), new SettingLabel("Facteur en bateau")));
        }
    }
}
