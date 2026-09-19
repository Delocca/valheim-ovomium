using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.FirstPerson
{
    /// <summary>
    /// Le point œil (<c>Character.m_eye</c>) est un transform que le jeu ne déplace jamais (il n'en pose que la
    /// rotation) : accroupi, la caméra vanilla reste à hauteur debout. Accroupi en vue subjective, la caméra est
    /// posée à une hauteur fixe au-dessus des pieds, <see cref="FirstPersonConfig.CrouchEyeHeight"/> — fixe comme
    /// le point œil debout, pour ne pas suivre le balancement de l'animation de marche furtive. Le lissage vanilla
    /// de <c>m_currentBaseOffset</c> (SmoothDamp 0,5 s) est trop lent pour la transition : en vue subjective, sa
    /// composante verticale est remplacée par un lissage court.
    /// </summary>
    internal static class FirstPersonCrouch
    {
        private const float SmoothTime = 0.1f;
        private static bool s_wasCrouching;
        private static bool s_smoothing;
        private static float s_currentY;
        private static float s_velocityY;

        /// <summary>Après le calcul vanilla de l'offset de base (point œil moins position du joueur).</summary>
        internal static void Apply(Player player, ref Vector3 offset)
        {
            if (!player.IsCrouching())
            {
                s_wasCrouching = false;
                return;
            }
            offset.y = FirstPersonConfig.CrouchEyeHeight.Value;
            if (s_wasCrouching)
                return;
            s_wasCrouching = true;
            Plugin.Log.LogInfo($"FirstPerson : accroupi, caméra à {offset.y:F2} m");
        }

        /// <summary>Après le lissage vanilla : remplace la composante verticale par un lissage court.</summary>
        internal static void Smooth(GameCamera camera, Player player, float dt)
        {
            float targetY = camera.GetCameraBaseOffset(player).y;
            if (!s_smoothing)
            {
                s_smoothing = true;
                s_currentY = camera.m_currentBaseOffset.y;
                s_velocityY = 0f;
            }
            s_currentY = Mathf.SmoothDamp(s_currentY, targetY, ref s_velocityY, SmoothTime, 999f, dt);
            camera.m_currentBaseOffset.y = s_currentY;
            camera.m_offsetBaseVel.y = s_velocityY;
        }

        /// <summary>Hors vue subjective : le lissage vanilla reprend depuis la valeur courante.</summary>
        internal static void StopSmoothing()
        {
            s_smoothing = false;
        }
    }

    /// <summary>Hauteur de la caméra accroupie.</summary>
    [HarmonyPatch(typeof(GameCamera), "GetCameraBaseOffset", typeof(Player))]
    internal static class FirstPersonCrouchOffsetPatch
    {
        private static void Postfix(Player player, ref Vector3 __result)
        {
            if (FirstPersonMode.Active && player == Player.m_localPlayer)
                FirstPersonCrouch.Apply(player, ref __result);
        }
    }

    /// <summary>Transition rapide entre debout et accroupi.</summary>
    [HarmonyPatch(typeof(GameCamera), "UpdateBaseOffset", typeof(Player), typeof(float))]
    internal static class FirstPersonCrouchSmoothPatch
    {
        private static void Postfix(GameCamera __instance, Player player, float dt)
        {
            if (FirstPersonMode.Active && player == Player.m_localPlayer)
                FirstPersonCrouch.Smooth(__instance, player, dt);
            else
                FirstPersonCrouch.StopSmoothing();
        }
    }
}
