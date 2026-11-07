using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.SlopeWalk
{
    /// <summary>Pente sous le joueur, calculée comme <c>Character.ApplySlide</c>.</summary>
    internal static class SlopeAngle
    {
        public static float Of(Character character)
        {
            Vector3 normal = character.m_groundTilt != Character.GroundTiltType.None
                ? character.m_groundTiltNormal : character.m_lastGroundNormal;
            return Mathf.Clamp(Mathf.Acos(Mathf.Clamp01(normal.y)) * Mathf.Rad2Deg, 0f, 90f);
        }
    }

    /// <summary>
    /// Jusqu'à <c>MaxAngle</c>, saute le dérapage vanilla (dès 38° pour un joueur) et son échappatoire, le wall-run en
    /// courant. <c>m_sliding</c> et <c>m_wallRunning</c> sont remis à faux à chaque frame par <c>UpdateMotion</c>.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.ApplySlide),
        new[] { typeof(float), typeof(Vector3), typeof(Vector3), typeof(bool) },
        new[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal })]
    internal static class SlopeWalkSlidePatch
    {
        private static bool Prefix(Character __instance)
        {
            if (!SlopeWalkConfig.Enabled.Value || __instance != Player.m_localPlayer
                || SlopeAngle.Of(__instance) > SlopeWalkConfig.MaxAngle.Value)
                return true;
            __instance.m_slippage = 0f;
            return false;
        }
    }

    /// <summary>
    /// Ralentissement en montée. <c>ApplyLiquidResistance</c> est le point de <c>UpdateWalking</c> (une fois par frame
    /// physique, seul appelant) où la vitesse du mode de marche est connue, avant les effets de statut.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyLiquidResistance),
        new[] { typeof(float) }, new[] { ArgumentType.Ref })]
    internal static class SlopeWalkSpeedPatch
    {
        /// <summary>Par seconde : assez lent pour ne pas saccader sur les bosses du terrain.</summary>
        private const float SmoothingRate = 3f;

        private static float s_slowdown;

        private static void Postfix(Character __instance, ref float speed)
        {
            if (__instance != Player.m_localPlayer)
                return;
            if (!SlopeWalkConfig.Enabled.Value)
            {
                s_slowdown = 0f;
                return;
            }
            s_slowdown = Mathf.MoveTowards(s_slowdown, TargetSlowdown(__instance), SmoothingRate * Time.fixedDeltaTime);
            speed *= 1f - s_slowdown;
        }

        private static float TargetSlowdown(Character character)
        {
            Vector3 move = character.m_moveDir;
            if (move == Vector3.zero || !character.IsOnGround() || character.IsAttached())
                return 0f;
            Vector3 normal = character.m_lastGroundNormal;
            float uphill = SlopeSlowdown.Uphill(move.x, move.z, normal.x, normal.z);
            return SlopeSlowdown.Target(SlopeAngle.Of(character), uphill, SlopeWalkConfig.MaxAngle.Value,
                SlopeWalkConfig.MaxSlowdownPercent.Value);
        }
    }
}
