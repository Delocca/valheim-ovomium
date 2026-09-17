using HarmonyLib;

namespace Ovomium.Features.StartupSkip
{
    /// <summary>
    /// Les logos sont affichés par la coroutine <c>SceneLoader.LoadSceneAsync</c> si <c>_showLogos</c>, que
    /// <c>Awake</c> force à vrai : on le remet à faux juste après (Start lance la coroutine ensuite).
    /// La vidéo d'introduction a son option vanilla depuis le patch du 2026-09-17 : plus rien à faire ici.
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
}
