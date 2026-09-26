using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.SmoothShading
{
    /// <summary>Réglages de la fonctionnalité « ombrage lisse ».</summary>
    internal static class SmoothShadingConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("SmoothShading", "Enabled", true,
                new ConfigDescription("Éclairage au rendu du jeu, sans les bandes de couleur que crée son ombrage en paliers "
                    + "sur le décor, les constructions et les personnages.",
                    null, new SettingLabel("Activé")));
        }
    }
}
