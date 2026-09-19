using HarmonyLib;

namespace Ovomium.Features.ContinueButton
{
    /// <summary>Pose le bouton au menu principal et mémorise le type de la partie lancée.</summary>
    internal static class ContinueButtonPatch
    {
        /// <summary><c>Start</c> vient après <c>Awake</c>, qui a rempli le menu ; les profils sont chargés.</summary>
        [HarmonyPatch(typeof(FejdStartup), "Start", new System.Type[0])]
        private static class StartPatch
        {
            private static void Postfix(FejdStartup __instance)
            {
                if (ContinueButtonConfig.Enabled.Value)
                    ContinueMenuButton.AddToMainMenu(__instance);
            }
        }

        /// <summary>Départ vers la partie : <c>m_startingWorld</c> est vrai pour un monde hébergé, faux pour un serveur.</summary>
        [HarmonyPatch(typeof(FejdStartup), "TransitionToMainScene", new System.Type[0])]
        private static class TransitionPatch
        {
            private static void Prefix(FejdStartup __instance)
            {
                LastSession.Remember(__instance);
            }
        }
    }
}
