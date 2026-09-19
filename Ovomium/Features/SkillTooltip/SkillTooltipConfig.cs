using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.SkillTooltip
{
    /// <summary>Réglages de la fonctionnalité « effet chiffré des compétences ».</summary>
    internal static class SkillTooltipConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("SkillTooltip", "Enabled", true,
                new ConfigDescription("Fenêtre des compétences : l'infobulle de chaque compétence indique son effet chiffré "
                    + "au niveau actuel et au niveau 100.",
                    null, new SettingLabel("Activé")));
        }
    }
}
