using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.EitrRadiation
{
    /// <summary>Réglages de la fonctionnalité « radiations d'Eitr inoffensives ».</summary>
    internal static class EitrRadiationConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("EitrRadiation", "Enabled", true,
                new ConfigDescription("Les particules lancées par la raffinerie d'Eitr en marche et par l'Eitr raffiné au sol ou sur un "
                    + "présentoir ne blessent plus personne (elles restent visibles).",
                    null, new SettingLabel("Activé")));
        }
    }
}
