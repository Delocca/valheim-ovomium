using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.ChestFill
{
    /// <summary>Réglages du remplissage des coffres et de l'inventaire de haut en bas.</summary>
    internal static class ChestFillConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> PlayerInventory { get; private set; }
        public static ConfigEntry<bool> GroupStacks { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("ChestFill", "Enabled", true,
                new ConfigDescription("Les objets déposés dans un coffre (Ctrl+clic, E maintenu, Tout empiler…) prennent la première case libre "
                    + "en partant du haut à gauche, au lieu du bas ; une pile à compléter est celle la plus en haut à gauche.",
                    null, new SettingLabel("Activé")));
            PlayerInventory = config.Bind("ChestFill", "PlayerInventory", true,
                new ConfigDescription("Même chose dans l'inventaire du joueur : les matériaux, nourriture, armures… remplissent les lignes 2 à 4 "
                    + "de haut en bas, la barre rapide (et les cases 9 et 0) en dernier. Armes et outils vont toujours en haut, comme dans le jeu.",
                    null, new SettingLabel("Aussi dans l'inventaire")));
            GroupStacks = config.Bind("ChestFill", "GroupStacks", true,
                new ConfigDescription("Une nouvelle pile d'un objet déjà présent (piles pleines) se pose à côté des piles existantes : "
                    + "au bout du plus long alignement horizontal (sinon vertical) ; ligne pleine → ligne du dessous, puis du dessus. "
                    + "Un coffre rangé en colonnes (plus de voisins verticaux qu'horizontaux) est traité en colonnes : au bout de la "
                    + "colonne, sinon colonne de droite. Indépendant du remplissage de haut en bas. Dans l'inventaire, la barre rapide "
                    + "et les cases 9 et 0 sont ignorées.",
                    null, new SettingLabel("Regrouper les piles")));
        }
    }
}
