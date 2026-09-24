using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.QuickStash
{
    /// <summary>Réglages de la fonctionnalité « rangement rapide » vers les coffres proches.</summary>
    internal static class QuickStashConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("QuickStash", "Enabled", true,
                new ConfigDescription("Inventaire ouvert, Ctrl + clic sur un objet le range dans le coffre le plus proche "
                    + "qui en contient déjà (portée de CraftFromChests) ; ce qui n'y rentre pas va aux coffres suivants "
                    + "qui en contiennent, le reste reste en inventaire (« C'est plein »). Sans coffre qui convienne, "
                    + "l'objet est jeté comme d'habitude. Glisser un objet hors de l'inventaire le jette toujours, comme dans le jeu.",
                    null, new SettingLabel("Activé")));
        }
    }
}
