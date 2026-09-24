using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.PickupFilter
{
    /// <summary>Réglages de la fonctionnalité « filtre du ramassage automatique ».</summary>
    internal static class PickupFilterConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("PickupFilter", "Enabled", true,
                new ConfigDescription("Inventaire ouvert, survoler un objet (inventaire ou coffre) et presser la touche du "
                    + "ramassage automatique (V) fait que ce type d'objet n'est plus ramassé automatiquement ; nouvel appui : "
                    + "réautorisé. Liste propre à chaque personnage. Le ramassage à la main (E) reste possible.",
                    null, new SettingLabel("Activé")));
        }
    }
}
