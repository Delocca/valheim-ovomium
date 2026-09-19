using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.AmbientOcclusion
{
    /// <summary>
    /// Intensité SSAO multipliée. <c>EnvMan.SetEnv</c> appelle <c>SetEnvironmentAOParams</c> à chaque FixedUpdate avec
    /// l'intensité de l'environnement courant : un changement de réglage s'applique donc aussitôt.
    /// </summary>
    [HarmonyPatch(typeof(CameraEffects), nameof(CameraEffects.SetEnvironmentAOParams), new System.Type[] { typeof(Color), typeof(float) })]
    internal static class AmbientOcclusionPatch
    {
        private static void Prefix(ref float intensity)
        {
            if (AmbientOcclusionConfig.Enabled.Value)
                intensity *= AmbientOcclusionConfig.Intensity.Value;
        }
    }
}
