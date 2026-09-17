using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Ovomium.Features.FastPortal;
using Ovomium.Features.FirstPerson;
using Ovomium.Features.FoodMarker;
using Ovomium.Features.FoodRecipeSort;
using Ovomium.Features.LoadingArt;
using Ovomium.Features.StartupSkip;
using Ovomium.Features.MapZoomToCursor;
using Ovomium.Features.MenuDoubleClick;
using Ovomium.Features.MinimapSize;
using Ovomium.Features.PasswordReveal;
using Ovomium.Features.RecipeKeyboardNav;
using Ovomium.Features.SettingsMenu;
using Ovomium.Features.StackDrag;

namespace Ovomium
{
    /// <summary>Point d'entrée BepInEx : charge la config de chaque fonctionnalité puis applique les patches.</summary>
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "ovo.ovomium";
        public const string Name = "Ovomium";
        /// <summary>Générée par le csproj depuis &lt;Version&gt; (obj/…/PluginVersion.g.cs).</summary>
        public const string Version = PluginVersion.Value;

        internal static ManualLogSource Log;
        /// <summary>Fichier de config du mod, parcouru par la fenêtre Ovomium (SettingsMenu).</summary>
        internal static ConfigFile ConfigFile;

        private void Awake()
        {
            Log = Logger;
            ConfigFile = Config;
            FoodRecipeSortConfig.Bind(Config);
            FoodMarkerConfig.Bind(Config);
            RecipeKeyboardNavConfig.Bind(Config);
            StackDragConfig.Bind(Config);
            FastPortalConfig.Bind(Config);
            MinimapSizeConfig.Bind(Config);
            MapZoomToCursorConfig.Bind(Config);
            MenuDoubleClickConfig.Bind(Config);
            PasswordRevealConfig.Bind(Config);
            FirstPersonConfig.Bind(Config);
            StartupSkipConfig.Bind(Config);
            LoadingArtConfig.Bind(Config);
            SettingsMenuConfig.Bind(Config);

            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo($"{Name} {Version} chargé");
        }
    }
}
