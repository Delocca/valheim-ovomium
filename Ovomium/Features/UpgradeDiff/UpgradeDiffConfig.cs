using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.UpgradeDiff
{
    /// <summary>Réglages de la fonctionnalité « diff de stats à l'amélioration ».</summary>
    internal static class UpgradeDiffConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("UpgradeDiff", "Enabled", true,
                new ConfigDescription("Onglet Amélioration : à côté de chaque valeur qui change (armure, durabilité, dégâts, blocage…), "
                    + "différence avec l'objet actuel en vert (+) ou rouge (−).",
                    null, new SettingLabel("Activé")));
        }
    }
}
