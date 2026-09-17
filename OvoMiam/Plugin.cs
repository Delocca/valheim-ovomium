using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using OvoMiam.Features.FoodMarker;
using OvoMiam.Features.FoodRecipeSort;
using OvoMiam.Features.RecipeKeyboardNav;

namespace OvoMiam
{
    /// <summary>Point d'entrée BepInEx : charge la config de chaque fonctionnalité puis applique les patches.</summary>
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "ovo.ovomiam";
        public const string Name = "OvoMiam";
        /// <summary>Générée par le csproj depuis &lt;Version&gt; (obj/…/PluginVersion.g.cs).</summary>
        public const string Version = PluginVersion.Value;

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            FoodRecipeSortConfig.Bind(Config);
            FoodMarkerConfig.Bind(Config);
            RecipeKeyboardNavConfig.Bind(Config);

            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo($"{Name} {Version} chargé");
        }
    }
}
