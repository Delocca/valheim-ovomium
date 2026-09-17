using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.FoodMarker
{
    /// <summary>Réglages de la fonctionnalité « marqueur coloré devant les plats ».</summary>
    internal static class FoodMarkerConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<string> Glyph { get; private set; }
        public static ConfigEntry<int> GlyphScale { get; private set; }

        public static void Bind(ConfigFile config)
        {
            const string section = "FoodMarker";
            Enabled = config.Bind(section, "Enabled", true,
                new ConfigDescription("Préfixer le nom des plats d'un point coloré par stat dominante : rouge vie, jaune endurance, bleu eitr (plusieurs points si mixte). " +
                    "Un plat cru est marqué selon sa version cuite.",
                    null, new SettingLabel("Activé")));
            Glyph = config.Bind(section, "Glyph", "●",
                "Caractère du marqueur. À changer si la police du jeu l'affiche comme un carré (ex. • ou ■).");
            GlyphScale = config.Bind(section, "GlyphScale", 70,
                new ConfigDescription("Taille du marqueur en pourcentage de la taille du texte.",
                    new AcceptableValueRange<int>(20, 200), new SettingLabel("Taille du marqueur (%)")));
        }
    }
}
