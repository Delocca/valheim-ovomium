using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;
using UnityEngine;

namespace Ovomium.Features.HotbarSlots
{
    /// <summary>Réglages de la fonctionnalité « emplacements rapides 9 et 0 ».</summary>
    internal static class HotbarSlotsConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> RecenterBar { get; private set; }
        public static ConfigEntry<KeyboardShortcut> Slot9Key { get; private set; }
        public static ConfigEntry<KeyboardShortcut> Slot10Key { get; private set; }
        /// <summary>Case de la grille de chaque emplacement, « colonne,ligne » à partir de 0 (ligne 0 = barre vanilla, exclue).</summary>
        public static ConfigEntry<string> Slot9Cell { get; private set; }
        public static ConfigEntry<string> Slot10Cell { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("HotbarSlots", "Enabled", true,
                new ConfigDescription("Deux emplacements rapides de plus : deux cases de l'inventaire (par défaut les deux "
                    + "premières de la 2e ligne) s'utilisent par les touches 9 et 0 et s'affichent dans la barre d'objets. "
                    + "Inventaire ouvert, survoler une case et presser la touche la choisit comme nouvelle cible.",
                    null, new SettingLabel("Activé")));
            RecenterBar = config.Bind("HotbarSlots", "RecenterBar", true,
                new ConfigDescription("Décale la barre d'objets d'une case vers la gauche pour qu'elle reste centrée à 10 cases.",
                    null, new SettingLabel("Barre recentrée")));
            Slot9Key = config.Bind("HotbarSlots", "Slot9Key", new KeyboardShortcut(KeyCode.Alpha9),
                "Touche du 9e emplacement rapide.");
            Slot10Key = config.Bind("HotbarSlots", "Slot10Key", new KeyboardShortcut(KeyCode.Alpha0),
                "Touche du 10e emplacement rapide.");
            Slot9Cell = config.Bind("HotbarSlots", "Slot9Cell", "0,1",
                "Case du 9e emplacement, « colonne,ligne » à partir de 0 (se règle en jeu : survol + touche).");
            Slot10Cell = config.Bind("HotbarSlots", "Slot10Cell", "1,1",
                "Case du 10e emplacement, « colonne,ligne » à partir de 0 (se règle en jeu : survol + touche).");
        }
    }
}
