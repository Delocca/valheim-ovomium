using BepInEx.Configuration;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>Réglages de la fonctionnalité « fenêtre Ovomium » (bouton dans les menus, options du mod en jeu).</summary>
    internal static class SettingsMenuConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> DumpHierarchy { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("SettingsMenu", "Enabled", true,
                "Bouton « Ovomium » dans le menu principal et le menu Échap, ouvrant les options du mod dans une fenêtre "
                + "au style du menu Paramètres.");
            DumpHierarchy = config.Bind("SettingsMenu", "DumpHierarchy", false,
                "Écrire dans le journal BepInEx la hiérarchie du prefab Paramètres à l'ouverture de la fenêtre. Pour le débogage.");
        }
    }
}
