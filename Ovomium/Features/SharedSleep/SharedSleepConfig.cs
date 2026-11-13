using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.SharedSleep
{
    /// <summary>Réglages de la fonctionnalité « nuit passée à la moitié du groupe couchée » (seuil fixe, voulu).</summary>
    internal static class SharedSleepConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("SharedSleep", "Enabled", true,
                new ConfigDescription("La nuit passe dès que la moitié des joueuses connectées (arrondie au-dessus) dort dans un lit. "
                    + "Il faut le mod chez les joueuses éveillées : une joueuse éveillée sans lui bloque la nuit comme dans le jeu.",
                    null, new SettingLabel("Activé")));
        }
    }
}
