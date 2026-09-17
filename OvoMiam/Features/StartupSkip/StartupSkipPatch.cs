using HarmonyLib;

namespace OvoMiam.Features.StartupSkip
{
    /// <summary>
    /// Les logos sont affichés par la coroutine <c>SceneLoader.LoadSceneAsync</c> si <c>_showLogos</c>, que
    /// <c>Awake</c> force à vrai : on le remet à faux juste après (Start lance la coroutine ensuite).
    /// </summary>
    [HarmonyPatch(typeof(SceneLoader), "Awake", new System.Type[0])]
    internal static class StartupSkipLogosPatch
    {
        private static void Postfix(SceneLoader __instance)
        {
            if (StartupSkipConfig.SkipLogos.Value)
                __instance._showLogos = false;
        }
    }

    /// <summary>
    /// <c>FejdStartup.PlayIntroCinematic</c> ne joue la vidéo que si <c>m_introOnStartup</c> (et au premier passage
    /// par le menu) ; sinon il affiche le menu directement, chemin vanilla du retour en menu.
    /// </summary>
    [HarmonyPatch(typeof(CinematicsManager), "Awake", new System.Type[0])]
    internal static class StartupSkipIntroPatch
    {
        private static void Postfix(CinematicsManager __instance)
        {
            if (!StartupSkipConfig.IntroVideo.Value)
                __instance.m_introOnStartup = false;
        }
    }
}
