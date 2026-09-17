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
            new Tab { Title = "Interface", Sections = new[] { "StackDrag", "MinimapSize", "MapZoomToCursor", "MenuDoubleClick", "PasswordReveal" } },
            new Tab { Title = "Jeu", Sections = new[] { "FastPortal", "FirstPerson", "StartupSkip", "LoadingArt" } },
        };

        private static readonly Dictionary<string, string> s_sectionLabels = new Dictionary<string, string>
        {
            { "FoodRecipeSort", "Tri des recettes" },
            { "FoodMarker", "Marqueur des plats" },
            { "RecipeKeyboardNav", "Recettes au clavier" },
            { "StackDrag", "Piles à la souris" },
            { "MinimapSize", "Taille de la minicarte" },
            { "MapZoomToCursor", "Zoom carte vers le curseur" },
            { "MenuDoubleClick", "Double-clic dans les menus" },
            { "PasswordReveal", "Mot de passe serveur" },
            { "FastPortal", "Portails rapides" },
            { "FirstPerson", "Vue subjective" },
            { "StartupSkip", "Démarrage rapide" },
            { "LoadingArt", "Artworks de chargement" },
        };

        public static string SectionLabel(string section) =>
            s_sectionLabels.TryGetValue(section, out var label) ? label : section;
    }
}
