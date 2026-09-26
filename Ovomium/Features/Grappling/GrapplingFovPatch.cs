using HarmonyLib;

namespace Ovomium.Features.Grappling
{
    /// <summary>
    /// <c>GrapplingPoint</c> (seul appelant) pose un FOV temporaire <c>m_FOVTarget</c> à l'accroche puis
    /// <c>ResetTempFOV</c> au décrochage. Sans <c>SetTempFOV</c>, le reset est sans effet (<c>m_fovBase</c> et
    /// <c>m_fovInertia</c> restent à 0) ; s'il a déjà servi, il ramène au FOV d'origine : désactiver en plein vol est sûr.
    /// </summary>
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.SetTempFOV), new System.Type[] { typeof(float), typeof(float) })]
    internal static class GrapplingFovPatch
    {
        private static bool Prefix() => !GrapplingConfig.FixedFov.Value;
    }
}
