using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.FastPortal
{
    /// <summary>Réglages de la fonctionnalité « portails rapides ».</summary>
    internal static class FastPortalConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> FadeSeconds { get; private set; }
        public static ConfigEntry<float> SettleSeconds { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("FastPortal", "Enabled", true,
                new ConfigDescription("Téléportation par portail dès que l'écran est noir et la zone d'arrivée chargée, sans l'attente fixe de 8 s.",
                    null, new SettingLabel("Activé")));
            FadeSeconds = config.Bind("FastPortal", "FadeSeconds", 0.5f,
                new ConfigDescription("Durée de chaque fondu au noir lors d'une téléportation, en secondes (vanilla : 1).",
                    new AcceptableValueRange<float>(0f, 3f), new SettingLabel("Durée du fondu (s)")));
            SettleSeconds = config.Bind("FastPortal", "SettleSeconds", 1f,
                new ConfigDescription("Quand la destination n'était pas déjà chargée au départ : délai supplémentaire, en secondes, après le "
                    + "chargement complet de l'aire d'arrivée (zones voisines et objets lointains compris).",
                    new AcceptableValueRange<float>(0f, 5f), new SettingLabel("Délai après chargement (s)")));
        }
    }
}
