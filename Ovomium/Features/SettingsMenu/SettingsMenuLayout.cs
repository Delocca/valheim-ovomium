using System.Collections.Generic;
using BepInEx.Configuration;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>
    /// Onglets de la fenêtre Ovomium : sections du .cfg regroupées (par ordre d'importance), et libellé français de
    /// chaque section. Toute section ayant une option étiquetée <see cref="SettingLabel"/> doit figurer dans un onglet.
    /// </summary>
    internal static class SettingsMenuLayout
    {
        public sealed class Tab
        {
            public string Title;
            public string[] Sections;
        }

        public static readonly Tab[] Tabs =
        {
            new Tab { Title = "Craft", Sections = new[] { "CraftFromChests", "ItemFlight", "FoodRecipeSort", "FoodMarker", "RecipeKeyboardNav", "UpgradeDiff", "DarkPrepTable" } },
            new Tab { Title = "Inventaire", Sections = new[] { "ChestFill", "QuickStash", "ManualChest", "StackDrag", "HotbarSlots", "UtilitySlots", "PickupFilter" } },
            new Tab { Title = "Monde", Sections = new[] { "MapExplore", "MinimapSize", "MapZoomToCursor", "FastPortal", "PortalRange", "Grappling", "ButcherKnife", "EitrRadiation" } },
            new Tab { Title = "Affichage", Sections = new[] { "FirstPerson", "SmoothShading", "AmbientOcclusion", "TooltipStyle", "SkillTooltip", "LoadingArt" } },
            new Tab { Title = "Menus", Sections = new[] { "StartupSkip", "ContinueButton", "MenuDoubleClick", "FocusClick", "PasswordReveal", "ServerWake", "Updater", "AutoJoin" } },
        };

        private static readonly Dictionary<string, string> s_sectionLabels = new Dictionary<string, string>
        {
            { "CraftFromChests", "Craft depuis les coffres" },
            { "ItemFlight", "Ingrédients qui volent" },
            { "FoodRecipeSort", "Tri des recettes" },
            { "FoodMarker", "Marqueur des plats" },
            { "RecipeKeyboardNav", "Recettes au clavier" },
            { "UpgradeDiff", "Diff de stats à l'amélioration" },
            { "DarkPrepTable", "Table de préparation sombre" },
            { "ChestFill", "Rangement de haut en bas" },
            { "QuickStash", "Rangement rapide" },
            { "ManualChest", "Coffres manuels" },
            { "StackDrag", "Piles à la souris" },
            { "HotbarSlots", "Emplacements rapides 9 et 0" },
            { "UtilitySlots", "Plusieurs objets utilitaires" },
            { "PickupFilter", "Filtre du ramassage auto" },
            { "MapExplore", "Rayon de découverte de la carte" },
            { "MinimapSize", "Taille de la minicarte" },
            { "MapZoomToCursor", "Zoom carte vers le curseur" },
            { "FastPortal", "Portails rapides" },
            { "PortalRange", "Rayon d'activation des portails" },
            { "Grappling", "Grappin" },
            { "ButcherKnife", "Couteau de boucher précis" },
            { "EitrRadiation", "Radiations d'Eitr inoffensives" },
            { "FirstPerson", "Vue subjective" },
            { "SmoothShading", "Ombrage lisse" },
            { "AmbientOcclusion", "Occlusion ambiante" },
            { "TooltipStyle", "Style des infobulles" },
            { "SkillTooltip", "Effet chiffré des compétences" },
            { "LoadingArt", "Artworks de chargement" },
            { "StartupSkip", "Démarrage rapide" },
            { "ContinueButton", "Bouton Continuer" },
            { "MenuDoubleClick", "Double-clic dans les menus" },
            { "FocusClick", "Clic de retour au jeu ignoré" },
            { "PasswordReveal", "Mot de passe serveur" },
            { "ServerWake", "Réveil des serveurs Nodecraft" },
            { "Updater", "Mises à jour" },
            { "AutoJoin", "Connexion automatique (dev)" },
        };

        public static string SectionLabel(string section) =>
            s_sectionLabels.TryGetValue(section, out var label) ? label : section;

        /// <summary>Garde-fou au chargement : une section étiquetée absente de tout onglet n'apparaîtrait nulle part.</summary>
        public static void WarnOrphans(ConfigFile config)
        {
            var placed = new HashSet<string>();
            foreach (var tab in Tabs)
                placed.UnionWith(tab.Sections);
            var warned = new HashSet<string>();
            foreach (var pair in config)
            {
                var section = pair.Key.Section;
                if (SettingLabel.Of(pair.Value) != null && !placed.Contains(section) && warned.Add(section))
                    Plugin.Log.LogWarning($"SettingsMenu : la section « {section} » a des options étiquetées mais n'est "
                        + "dans aucun onglet (SettingsMenuLayout.Tabs) : absente de la fenêtre Ovomium");
            }
        }
    }
}
