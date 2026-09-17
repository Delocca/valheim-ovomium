using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Ovomium.Features.LoadingArt
{
    /// <summary>
    /// Écran de démarrage (logo du jeu + indicateur de chargement sur fond noir) : artwork juste sous le logo dans sa
    /// propre hiérarchie, ce qui est derrière lui (fond noir vanilla) reste derrière, ce qui est devant reste devant.
    /// </summary>
    [HarmonyPatch(typeof(SceneLoader), "Awake", new System.Type[0])]
    internal static class LoadingArtStartupPatch
    {
        private static void Postfix(SceneLoader __instance)
        {
            LoadingArtDownloader.StartIfNeeded(); // premier point d'entrée du jeu : une fois par session
            if (!LoadingArtLibrary.Available)
                return;
            Transform logo = __instance.gameLogo.transform;
            LoadingArtView.Apply(LoadingArtView.Install(logo.parent, logo.GetSiblingIndex()));
        }
    }

    /// <summary>
    /// Écran « Loading » du menu, affiché pendant le chargement synchrone de la scène principale : préparé à l'Awake,
    /// image tirée au moment de l'affichage.
    /// </summary>
    [HarmonyPatch(typeof(FejdStartup))]
    internal static class LoadingArtMenuPatch
    {
        private static Image s_art;

        [HarmonyPostfix, HarmonyPatch("Awake", new System.Type[0])]
        private static void AwakePostfix(FejdStartup __instance)
        {
            s_art = null;
            if (LoadingArtLibrary.Available && __instance.m_loading != null)
                s_art = LoadingArtView.Install(__instance.m_loading.transform, 0);
        }

        [HarmonyPrefix, HarmonyPatch("LoadMainScene", new System.Type[0])]
        private static void LoadMainScenePrefix()
        {
            if (s_art != null)
                LoadingArtView.Apply(s_art);
        }
    }

    /// <summary>
    /// Écran de chargement en jeu (<c>Hud.m_loadingScreen</c>) : actif dès la connexion au serveur (joueur absent),
    /// à la mort, au sommeil, à la téléportation et au chargement. L'image vanilla <c>m_loadingImage</c> n'est visible
    /// que dans la branche des astuces : on installe notre artwork directement sous l'écran, nouvelle image à chaque
    /// apparition.
    /// </summary>
    [HarmonyPatch(typeof(Hud), "UpdateBlackScreen", typeof(Player), typeof(float))]
    internal static class LoadingArtHudPatch
    {
        private static bool s_shown;
        private static Image s_art;

        private static void Postfix(Hud __instance)
        {
            bool shown = __instance.m_loadingScreen.gameObject.activeSelf;
            if (s_art == null)
                s_shown = false; // nouveau Hud (retour au menu puis nouvelle partie) : l'ancien artwork a été détruit avec lui
            if (shown && !s_shown && LoadingArtLibrary.Available)
            {
                if (s_art == null)
                    Install(__instance);
                LoadingArtView.Apply(s_art);
            }
            s_shown = shown;
        }

        private static void Install(Hud hud)
        {
            Plugin.Log.LogInfo("LoadingArt : hiérarchie de l'écran de chargement en jeu");
            LoadingArtView.LogHierarchy(hud.m_loadingScreen.transform, hud.m_loadingProgress.transform,
                hud.m_teleportingProgress.transform, hud.m_sleepingProgress.transform);
            s_art = LoadingArtView.Install(hud.m_loadingScreen.transform, 0);
            if (hud.m_loadingImage != null)
                hud.m_loadingImage.enabled = false;
            if (LoadingArtConfig.HideTeleportAnimation.Value)
            {
                int hidden = LoadingArtView.HideSprites(hud.m_teleportingProgress.transform, "teleport");
                Plugin.Log.LogInfo($"LoadingArt : {hidden} image(s) de téléportation masquée(s)");
                if (hidden == 0)
                    foreach (Image image in hud.m_teleportingProgress.GetComponentsInChildren<Image>(true))
                        Plugin.Log.LogInfo($"LoadingArt :   téléportation : {image.name} sprite={(image.sprite == null ? "-" : image.sprite.name)}");
            }
        }
    }
}
