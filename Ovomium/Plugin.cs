using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Ovomium.Features.AmbientOcclusion;
using Ovomium.Features.AutoJoin;
using Ovomium.Features.ButcherKnife;
using Ovomium.Features.ContinueButton;
using Ovomium.Features.CraftFromChests;
using Ovomium.Features.DarkPrepTable;
using Ovomium.Features.EitrRadiation;
using Ovomium.Features.FastPortal;
using Ovomium.Features.FirstPerson;
using Ovomium.Features.FocusClick;
using Ovomium.Features.FoodMarker;
using Ovomium.Features.FoodRecipeSort;
using Ovomium.Features.GraphicsPreview;
using Ovomium.Features.HotbarSlots;
using Ovomium.Features.ChestFill;
using Ovomium.Features.LoadingArt;
using Ovomium.Features.ManualChest;
using Ovomium.Features.MapExplore;
using Ovomium.Features.StartupSkip;
using Ovomium.Features.MapZoomToCursor;
using Ovomium.Features.MenuDoubleClick;
using Ovomium.Features.MinimapSize;
using Ovomium.Features.PasswordReveal;
using Ovomium.Features.PickupFilter;
using Ovomium.Features.PortalRange;
using Ovomium.Features.QuickStash;
using Ovomium.Features.RecipeKeyboardNav;
using Ovomium.Features.ServerWake;
using Ovomium.Features.SettingsMenu;
using Ovomium.Features.SkillTooltip;
using Ovomium.Features.ItemFlight;
using Ovomium.Features.StackDrag;
using Ovomium.Features.TooltipStyle;
using Ovomium.Features.Updater;
using Ovomium.Features.UpgradeDiff;
using UnityEngine;

namespace Ovomium
{
    /// <summary>
    /// Point d'entrée du cœur, appelé par le chargeur (Ovomium.Loader, seul plugin BepInEx) : charge la config de
    /// chaque fonctionnalité puis applique les patches. Contrat stable avec le chargeur (réflexion, signatures à ne
    /// pas changer) : <see cref="Load"/> et <see cref="Unload"/>.
    /// </summary>
    public static class Plugin
    {
        public const string Guid = "ovo.ovomium";
        public const string Name = "Ovomium";
        /// <summary>Générée par le csproj depuis &lt;Version&gt; (obj/…/PluginVersion.g.cs).</summary>
        public const string Version = PluginVersion.Value;

        internal static ManualLogSource Log;
        /// <summary>Fichier de config du mod, parcouru par la fenêtre Ovomium (SettingsMenu). Un nouveau par
        /// chargement : les entrées de la version déchargée (et leurs abonnements <c>SettingChanged</c>) restent
        /// attachées à l'ancien, que plus rien ne modifie.</summary>
        internal static ConfigFile ConfigFile;
        /// <summary>Demande au chargeur de remplacer ce cœur par la DLL indiquée, à la frame suivante (Updater).</summary>
        internal static System.Action<string> RequestReload;

        private static Harmony s_harmony;

        public static void Load(GameObject host, ManualLogSource log, System.Action<string> requestReload)
        {
            Log = log;
            RequestReload = requestReload;
            ConfigFile = new ConfigFile(Path.Combine(Paths.ConfigPath, Guid + ".cfg"), true, new BepInPlugin(Guid, Name, Version));
            BindAll(ConfigFile);
            // Identifiant propre à cette assembly (nom unique par chargement) : l'UnpatchSelf d'une version ne
            // retire jamais les patches d'une autre.
            s_harmony = new Harmony(typeof(Plugin).Assembly.GetName().Name);
            PatchInstaller.Install(s_harmony);
            OvomiumMenuButton.Install();  // rechargement à chaud : les menus existent déjà, leurs Start ne rejouent pas
            ContinueMenuButton.Install();
            FocusClickPatch.Install(host);
            UpdaterPatch.Install(host);
            PortalRangePatch.Install();
            DarkPrepTablePatch.Install();
            ItemFlightPatch.Install();
            ManualChestButton.Install();
            Log.LogInfo($"{Name} {Version} chargé");
            UpdaterPatch.AfterLoad();
        }

        private static void BindAll(ConfigFile config)
        {
            FoodRecipeSortConfig.Bind(config);
            FoodMarkerConfig.Bind(config);
            RecipeKeyboardNavConfig.Bind(config);
            StackDragConfig.Bind(config);
            FastPortalConfig.Bind(config);
            MinimapSizeConfig.Bind(config);
            MapZoomToCursorConfig.Bind(config);
            MenuDoubleClickConfig.Bind(config);
            PasswordRevealConfig.Bind(config);
            ServerWakeConfig.Bind(config);
            FirstPersonConfig.Bind(config);
            StartupSkipConfig.Bind(config);
            LoadingArtConfig.Bind(config);
            AutoJoinConfig.Bind(config);
            ContinueButtonConfig.Bind(config);
            FocusClickConfig.Bind(config);
            MapExploreConfig.Bind(config);
            AmbientOcclusionConfig.Bind(config);
            HotbarSlotsConfig.Bind(config);
            ChestFillConfig.Bind(config);
            QuickStashConfig.Bind(config);
            ManualChestConfig.Bind(config);
            PortalRangeConfig.Bind(config);
            DarkPrepTableConfig.Bind(config);
            ButcherKnifeConfig.Bind(config);
            EitrRadiationConfig.Bind(config);
            UpgradeDiffConfig.Bind(config);
            CraftFromChestsConfig.Bind(config);
            PickupFilterConfig.Bind(config);
            ItemFlightConfig.Bind(config);
            SkillTooltipConfig.Bind(config);
            TooltipStyleConfig.Bind(config);
            SettingsMenuConfig.Bind(config);
            UpdaterConfig.Bind(config);
        }

        /// <summary>
        /// Rechargement à chaud (mise à jour au menu, mode dev) ou fermeture du jeu : retire les patches et les effets
        /// posés sur la scène avant que la nouvelle version ne s'installe, sinon les deux versions tournent ensemble.
        /// Le chargeur détruit ensuite l'objet hôte (guetteurs).
        /// </summary>
        public static void Unload()
        {
            FirstPersonMode.Unload();
            LoadingArtView.Unload();
            OvomiumMenuButton.Unload();
            ContinueMenuButton.Unload();
            PasswordRevealPatch.Unload();
            ServerWakePatch.Unload();
            MinimapSizePatch.Unload();
            PortalRangePatch.Unload();
            DarkPrepTablePatch.Unload();
            TooltipStylePatch.Unload();
            UpdaterPatch.Unload();
            ChestReservation.Unload();
            QuickStashQueue.Unload();
            RequirementBackdrop.Unload();
            ItemFlightPatch.Unload();
            ManualChestButton.Unload();
            PickupFilterBadge.Unload();
            GraphicsPreview.Unload();
            s_harmony?.UnpatchSelf();
            s_harmony = null;
            Log.LogInfo($"{Name} {Version} déchargé");
        }
    }
}
