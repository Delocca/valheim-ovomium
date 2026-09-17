using BepInEx.Configuration;

namespace OvoMiam.Features.MapZoomToCursor
{
    /// <summary>Réglages de la fonctionnalité « zoom de la grande carte vers le curseur ».</summary>
    internal static class MapZoomToCursorConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("MapZoomToCursor", "Enabled", true,
                "Sur la grande carte, le zoom (molette, touches de zoom) se fait autour du point sous le curseur "
                + "au lieu du centre de l'écran. Sans effet à la manette ou au tactile.");
        }
    }
}
