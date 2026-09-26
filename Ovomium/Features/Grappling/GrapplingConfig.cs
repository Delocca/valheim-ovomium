using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.Grappling
{
    /// <summary>Réglages du grappin : champ de vision fixe, traction au clic.</summary>
    internal static class GrapplingConfig
    {
        public static ConfigEntry<bool> FixedFov { get; private set; }
        public static ConfigEntry<bool> ClickPull { get; private set; }

        public static void Bind(ConfigFile config)
        {
            FixedFov = config.Bind("Grappling", "FixedFov", true,
                new ConfigDescription("Le champ de vision ne change plus pendant qu'on se tire au grappin.",
                    null, new SettingLabel("Champ de vision fixe")));
            ClickPull = config.Bind("Grappling", "ClickPull", true,
                new ConfigDescription("Grappin accroché, le clic gauche (attaque) relance la traction vers le point d'accroche, "
                    + "même au sol, pour le même coût d'endurance qu'un saut.",
                    null, new SettingLabel("Traction au clic")));
        }
    }
}
