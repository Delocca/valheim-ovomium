using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Ovomium.Features.AmbientOcclusion;
using Ovomium.Features.AutoJoin;
using Ovomium.Features.ButcherKnife;
using Ovomium.Features.ContinueButton;
using Ovomium.Features.FastPortal;
using Ovomium.Features.FirstPerson;
using Ovomium.Features.FocusClick;
using Ovomium.Features.FoodMarker;
using Ovomium.Features.FoodRecipeSort;
using Ovomium.Features.LoadingArt;
using Ovomium.Features.MapExplore;
using Ovomium.Features.StartupSkip;
using Ovomium.Features.MapZoomToCursor;
using Ovomium.Features.MenuDoubleClick;
using Ovomium.Features.MinimapSize;
using Ovomium.Features.PasswordReveal;
using Ovomium.Features.PortalRange;
using Ovomium.Features.RecipeKeyboardNav;
using Ovomium.Features.SettingsMenu;
using Ovomium.Features.SkillTooltip;
using Ovomium.Features.StackDrag;
using Ovomium.Features.UpgradeDiff;

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

        private Harmony m_harmony;

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
            AutoJoinConfig.Bind(Config);
            ContinueButtonConfig.Bind(Config);
            FocusClickConfig.Bind(Config);
            MapExploreConfig.Bind(Config);
            AmbientOcclusionConfig.Bind(Config);
            PortalRangeConfig.Bind(Config);
            ButcherKnifeConfig.Bind(Config);
            UpgradeDiffConfig.Bind(Config);
            SkillTooltipConfig.Bind(Config);
            SettingsMenuConfig.Bind(Config);

            m_harmony = new Harmony(Guid);
            PatchInstaller.Install(m_harmony);
            OvomiumMenuButton.Install();  // rechargement à chaud : les menus existent déjà, leurs Start ne rejouent pas
            ContinueMenuButton.Install();
            FocusClickPatch.Install(gameObject);
            PortalRangePatch.Install();
            Log.LogInfo($"{Name} {Version} chargé");
        }

        /// <summary>
        /// Rechargement à chaud (ScriptEngine, tools/deploy.sh --dev) : retire les patches et les effets posés sur la
        /// scène avant que la nouvelle version ne s'installe, sinon les deux versions tournent ensemble.
        /// </summary>
        private void OnDestroy()
        {
            FirstPersonMode.Unload();
            LoadingArtView.Unload();
            OvomiumMenuButton.Unload();
            ContinueMenuButton.Unload();
            PasswordRevealPatch.Unload();
            MinimapSizePatch.Unload();
            PortalRangePatch.Unload();
            m_harmony?.UnpatchSelf();
            Log.LogInfo($"{Name} {Version} déchargé");
        }
    }
}
