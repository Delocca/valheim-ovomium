using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.SlopeWalk
{
    /// <summary>Réglages de la fonctionnalité « pentes sans dérapage ».</summary>
    internal static class SlopeWalkConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<int> MaxAngle { get; private set; }
        public static ConfigEntry<int> MaxSlowdownPercent { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("SlopeWalk", "Enabled", true,
                new ConfigDescription("Supprime le dérapage dans les pentes, remplacé par un léger ralentissement en montée.",
                    null, new SettingLabel("Activé")));
            MaxAngle = config.Bind("SlopeWalk", "MaxAngle", 60,
                new ConfigDescription("Pente, en degrés, jusqu'à laquelle on ne dérape plus (le jeu dérape dès 38°). Au-delà, comportement du jeu : "
                    + "on y passe en courant, avec de l'endurance.",
                    new AcceptableValueRange<int>(38, 85), new SettingLabel("Pente maximale (°)")));
            MaxSlowdownPercent = config.Bind("SlopeWalk", "MaxSlowdownPercent", 30,
                new ConfigDescription("Ralentissement atteint à la pente maximale en montant droit dans la pente, en pourcentage de la vitesse. "
                    + "Progressif à partir de 20°, nul en traversée et en descente.",
                    new AcceptableValueRange<int>(0, 75), new SettingLabel("Ralentissement maximal en montée (%)")));
        }
    }
}
