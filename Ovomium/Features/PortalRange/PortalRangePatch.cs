using HarmonyLib;

namespace Ovomium.Features.PortalRange
{
    /// <summary>
    /// Rayon d'activation posé sur chaque portail à son réveil (<c>TeleportWorld.m_activationRange</c>, lu par
    /// <c>UpdatePortal</c> toutes les 0,5 s). Un changement de réglage est répercuté sur les portails en scène ;
    /// la valeur vanilla, mémorisée au premier portail rencontré, est remise au déchargement.
    /// </summary>
    [HarmonyPatch(typeof(TeleportWorld), "Awake", new System.Type[0])]
    internal static class PortalRangePatch
    {
        private static float? s_vanillaRange;

        private static void Postfix(TeleportWorld __instance) => Apply(__instance);

        /// <summary>Rechargement à chaud : portails déjà en scène, et suivi des réglages.</summary>
        internal static void Install()
        {
            PortalRangeConfig.Enabled.SettingChanged += (_, __) => ApplyAll();
            PortalRangeConfig.ActivationRange.SettingChanged += (_, __) => ApplyAll();
            ApplyAll();
        }

        internal static void Unload()
        {
            if (s_vanillaRange == null)
                return;
            foreach (TeleportWorld portal in LivePortals())
                portal.m_activationRange = s_vanillaRange.Value;
        }

        private static void ApplyAll()
        {
            foreach (TeleportWorld portal in LivePortals())
                Apply(portal);
        }

        private static TeleportWorld[] LivePortals()
            => UnityEngine.Object.FindObjectsByType<TeleportWorld>(UnityEngine.FindObjectsSortMode.None);

        private static void Apply(TeleportWorld portal)
        {
            if (s_vanillaRange == null)
                s_vanillaRange = portal.m_activationRange;
            portal.m_activationRange = PortalRangeConfig.Enabled.Value ? PortalRangeConfig.ActivationRange.Value : s_vanillaRange.Value;
        }
    }
}
