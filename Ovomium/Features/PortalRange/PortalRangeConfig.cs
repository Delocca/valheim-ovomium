using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.PortalRange
{
    /// <summary>Réglages de la fonctionnalité « rayon d'activation des portails ».</summary>
    internal static class PortalRangeConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> ActivationRange { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("PortalRange", "Enabled", true,
                new ConfigDescription("Réduit la distance à laquelle un portail s'allume à l'approche d'un joueur.",
                    null, new SettingLabel("Activé")));
            ActivationRange = config.Bind("PortalRange", "ActivationRange", 2f,
                new ConfigDescription("Distance, en mètres, à laquelle un joueur allume le halo et le bourdonnement du portail (vanilla : 5).",
                    new AcceptableValueRange<float>(0.5f, 10f), new SettingLabel("Rayon d'activation (m)")));
        }
    }
}
