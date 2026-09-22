using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.ManualChest
{
    /// <summary>Réglages des coffres manuels (bouton « Coffre auto » du panneau coffre, E maintenu coffre ouvert).</summary>
    internal static class ManualChestConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("ManualChest", "Enabled", true,
                new ConfigDescription("Le bouton « Objets similaires » du panneau coffre devient « Coffre auto : oui / non » : "
                    + "un coffre manuel est ignoré par le rangement rapide, la fabrication et le combustible depuis les "
                    + "coffres (le réglage est mémorisé sur le coffre). Ranger les objets similaires se fait alors en "
                    + "maintenant E (coffre ouvert) sans fermer le coffre ; un appui bref le ferme comme d'habitude. "
                    + "Désactivé : bouton et E vanilla, un coffre déjà marqué manuel le reste.",
                    null, new SettingLabel("Activé")));
        }
    }
}
