using BepInEx.Configuration;

namespace OvoMiam.Features.RecipeKeyboardNav
{
    /// <summary>Réglages de la fonctionnalité « navigation clavier dans la liste des recettes ».</summary>
    internal static class RecipeKeyboardNavConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("RecipeKeyboardNav", "Enabled", true,
                "Flèches haut/bas pour changer de recette sélectionnée dans toute station d'artisanat.");
        }
    }
}
