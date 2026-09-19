using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.ButcherKnife
{
    /// <summary>Réglages de la fonctionnalité « couteau de boucher ».</summary>
    internal static class ButcherKnifeConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("ButcherKnife", "Enabled", true,
                new ConfigDescription("Le couteau de boucher (et toute arme réservée aux animaux apprivoisés) ne frappe que la créature "
                    + "visée par le joueur ; sans créature visée, le coup ne touche rien.",
                    null, new SettingLabel("Activé")));
        }
    }
}
