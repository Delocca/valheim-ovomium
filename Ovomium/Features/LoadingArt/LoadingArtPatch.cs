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
    /// image tirée dès <c>TransitionToMainScene</c> (Démarrer / Rejoindre) et réservée pour l'écran en jeu qui suit
    /// (une seule image par chargement de partie). Le clip <c>startmenu_fadeout</c> du menu (déclencheur « FadeOut »,
    /// posé par TransitionToMainScene) active <c>m_loading</c> lui-même et fond son CanvasGroup de 0 à 1 en 1,5 s,
    /// bien avant LoadMainScene (attente du backend serveur, parfois 10 s) : l'image doit donc être en place à ce
    /// moment, sinon l'artwork sans sprite se dessine en blanc uni jusqu'au chargement, et la dernière image
    /// présentée avant le gel synchrone est celle-là. Le prefix de LoadMainScene reste en secours (hiérarchie
    /// retrouvée ou recréée : après un rechargement à chaud dans le menu, l'Awake n'est pas rejoué).
    /// </summary>
    [HarmonyPatch(typeof(FejdStartup))]
    internal static class LoadingArtMenuPatch
    {
        [HarmonyPostfix, HarmonyPatch("Awake", new System.Type[0])]
        private static void AwakePostfix(FejdStartup __instance)
        {
            if (LoadingArtLibrary.Available && __instance.m_loading != null)
                LoadingArtView.Install(__instance.m_loading.transform, 0);
        }

        [HarmonyPrefix, HarmonyPatch("TransitionToMainScene", new System.Type[0])]
        private static void TransitionPrefix(FejdStartup __instance) => PrepareMenuArt(__instance, force: true);

        [HarmonyPrefix, HarmonyPatch("LoadMainScene", new System.Type[0])]
        private static void LoadMainScenePrefix(FejdStartup __instance) => PrepareMenuArt(__instance, force: false);

        /// <summary>
        /// Tire et pose l'image du menu ; sans <paramref name="force"/>, seulement si l'artwork n'en a pas.
        /// (Pas « Prepare » : nom réservé par Harmony dans une classe de patch.)
        /// </summary>
        private static void PrepareMenuArt(FejdStartup startup, bool force)
        {
            if (!LoadingArtLibrary.Available || startup.m_loading == null)
                return;
            Image art = LoadingArtView.Install(startup.m_loading.transform, 0);
            if (force || LoadingArtView.IsBlank(art))
            {
                LoadingArtView.Apply(art);
                LoadingArtLibrary.KeepForGame();
            }
        }
    }

    /// <summary>
    /// Écran de chargement en jeu (<c>Hud.m_loadingScreen</c>) : actif dès la connexion au serveur (joueur absent),
    /// à la mort, au sommeil, à la téléportation et au chargement. L'image vanilla <c>m_loadingImage</c> n'est visible
    /// que dans la branche des astuces : on installe notre artwork directement sous l'écran, nouvelle image à chaque
    /// apparition (sauf la première d'une partie, qui reprend celle du menu). Un artwork devenu blanc pendant
    /// l'affichage (sprite ou texture détruits) est retiré à nouveau, avec avertissement.
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
            if (shown && LoadingArtLibrary.Available)
            {
                if (!s_shown)
                {
                    if (s_art == null)
                        Install(__instance);
                    LoadingArtView.Apply(s_art, LoadingArtLibrary.NextForGame());
                }
                else if (LoadingArtView.IsBlank(s_art))
                {
                    Plugin.Log.LogWarning("LoadingArt : artwork en jeu devenu blanc (sprite ou texture détruit), nouveau tirage");
                    LoadingArtView.Apply(s_art);
                }
            }
            s_shown = shown;
        }

        private static void Install(Hud hud)
        {
            Plugin.Log.LogInfo("LoadingArt : hiérarchie de l'écran de chargement en jeu");
            LoadingArtView.LogHierarchy(hud.m_loadingScreen.transform, hud.m_loadingProgress.transform,
                hud.m_teleportingProgress.transform, hud.m_sleepingProgress.transform);
            s_art = LoadingArtView.Install(hud.m_loadingScreen.transform, 0);
            LoadingArtView.Hide(hud.m_loadingImage);
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
