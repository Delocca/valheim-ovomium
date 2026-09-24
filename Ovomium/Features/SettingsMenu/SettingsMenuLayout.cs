using System.Collections.Generic;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>Onglets de la fenêtre Ovomium : sections du .cfg regroupées, et libellé français de chaque section.</summary>
    internal static class SettingsMenuLayout
    {
        public sealed class Tab
        {
            public string Title;
            public string[] Sections;
        }

        public static readonly Tab[] Tabs =
        {
            new Tab { Title = "Cuisine", Sections = new[] { "FoodRecipeSort", "FoodMarker", "RecipeKeyboardNav" } },
            new Tab { Title = "Interface", Sections = new[] { "StackDrag", "HotbarSlots", "ChestFill", "QuickStash", "ManualChest", "MinimapSize", "MapZoomToCursor", "MapExplore", "TooltipStyle", "SkillTooltip", "MenuDoubleClick", "PasswordReveal" } },
            new Tab { Title = "Jeu", Sections = new[] { "FastPortal", "PortalRange", "ButcherKnife", "CraftFromChests", "PickupFilter", "ItemFlight", "UpgradeDiff", "AmbientOcclusion", "FirstPerson", "StartupSkip", "LoadingArt", "Updater", "AutoJoin" } },
        };

        private static readonly Dictionary<string, string> s_sectionLabels = new Dictionary<string, string>
        {
            { "FoodRecipeSort", "Tri des recettes" },
            { "FoodMarker", "Marqueur des plats" },
            { "RecipeKeyboardNav", "Recettes au clavier" },
            { "StackDrag", "Piles à la souris" },
            { "HotbarSlots", "Emplacements rapides 9 et 0" },
            { "ChestFill", "Rangement de haut en bas" },
            { "QuickStash", "Rangement rapide" },
            { "ManualChest", "Coffres manuels" },
            { "MinimapSize", "Taille de la minicarte" },
            { "MapZoomToCursor", "Zoom carte vers le curseur" },
            { "MapExplore", "Rayon de découverte de la carte" },
            { "TooltipStyle", "Style des infobulles" },
            { "SkillTooltip", "Effet chiffré des compétences" },
            { "PortalRange", "Rayon d'activation des portails" },
            { "ButcherKnife", "Couteau de boucher précis" },
            { "CraftFromChests", "Craft depuis les coffres" },
            { "PickupFilter", "Filtre du ramassage auto" },
            { "ItemFlight", "Ingrédients qui volent" },
            { "UpgradeDiff", "Diff de stats à l'amélioration" },
            { "AmbientOcclusion", "Occlusion ambiante" },
            { "MenuDoubleClick", "Double-clic dans les menus" },
            { "PasswordReveal", "Mot de passe serveur" },
            { "FastPortal", "Portails rapides" },
            { "FirstPerson", "Vue subjective" },
            { "StartupSkip", "Démarrage rapide" },
            { "LoadingArt", "Artworks de chargement" },
            { "Updater", "Mises à jour" },
            { "AutoJoin", "Connexion automatique (dev)" },
        };

        public static string SectionLabel(string section) =>
            s_sectionLabels.TryGetValue(section, out var label) ? label : section;
    }
}
