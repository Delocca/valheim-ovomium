using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.TooltipStyle
{
    /// <summary>Réglages de la fonctionnalité « style des infobulles ».</summary>
    internal static class TooltipStyleConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> BackgroundOpacity { get; private set; }
        public static ConfigEntry<int> CornerRadius { get; private set; }
        public static ConfigEntry<float> BorderOpacity { get; private set; }
        public static ConfigEntry<bool> LogHierarchy { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("TooltipStyle", "Enabled", true,
                new ConfigDescription("Infobulles cadrées (objets, craft, compétences) : fond plus opaque, coins arrondis, liseré discret.",
                    null, new SettingLabel("Activé")));
            BackgroundOpacity = config.Bind("TooltipStyle", "BackgroundOpacity", 1f,
                new ConfigDescription("Opacité du fond (0 : invisible, 1 : opaque ; le jeu teinte son fond à 0.95 sur un sprite "
                    + "lui-même translucide).",
                    new AcceptableValueRange<float>(0f, 1f), new SettingLabel("Opacité du fond")));
            CornerRadius = config.Bind("TooltipStyle", "CornerRadius", 14,
                new ConfigDescription("Rayon des coins en pixels (0 : forme vanilla, sans liseré).",
                    new AcceptableValueRange<int>(0, 32), new SettingLabel("Rayon des coins (px)")));
            BorderOpacity = config.Bind("TooltipStyle", "BorderOpacity", 0.35f,
                new ConfigDescription("Opacité du liseré clair de 1 px (0 : sans liseré).",
                    new AcceptableValueRange<float>(0f, 1f), new SettingLabel("Opacité du liseré")));
            LogHierarchy = config.Bind("TooltipStyle", "LogHierarchy", false,
                "Diagnostic : écrit dans le journal la hiérarchie de chaque infobulle créée (objets, images, tailles).");
        }
    }
}
