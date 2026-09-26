using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.Grappling
{
    /// <summary>
    /// Grappin accroché (<c>GrapplingPoint.m_localGrappler</c>), le clic gauche relance la traction comme le saut en
    /// l'air du jeu (<c>Character.Jump</c> : distance &gt; <c>m_jumpPullMaxDist</c>, <c>Pull()</c>, coût d'un saut), mais
    /// aussi au sol et dès <c>m_closeBreakDist</c>. L'attaque est neutralisée tant que le grappin tient : l'arbalète à
    /// grappin, vide, ne peut de toute façon pas tirer (rechargement bloqué par <c>Player.m_grappling</c>).
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls), new System.Type[] {
        typeof(Vector3), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool),
        typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    internal static class GrapplingClickPatch
    {
        private static void Prefix(Player __instance, ref bool attack, ref bool attackHold)
        {
            if (!GrapplingConfig.ClickPull.Value || __instance != Player.m_localPlayer) return;
            GrapplingPoint point = GrapplingPoint.m_localGrappler;
            if (point == null || !point.m_nview.IsOwner()) return;
            if (attack) TryPull(__instance, point);
            attack = false;
            attackHold = false;
        }

        private static void TryPull(Player player, GrapplingPoint point)
        {
            if (!point.m_secondary || point.m_anim == null) return;  // traction déjà en cours
            if (Vector3.Distance(point.transform.position, player.transform.position) <= point.m_closeBreakDist) return;
            float usage = player.m_jumpStaminaUsage;
            if (!player.HaveStamina(usage)) Hud.instance.StaminaBarEmptyFlash();
            player.UseStamina(usage - usage * player.GetEquipmentMovementModifier() + usage * player.GetEquipmentJumpStaminaModifier());
            point.Pull();
        }
    }

    /// <summary>
    /// Au sol, <c>Character.UpdateWalking</c> impose la vitesse de marche (et plafonne la verticale à 3 m/s) par-dessus
    /// la vitesse posée par <c>GrapplingPoint.Update</c> : vers un point pas plus haut que soi la distance ne baisse
    /// pas et la traction s'arrête au bout de <c>m_breakEarlyTime</c>. Pendant une traction relancée (<c>Pull</c> : clic
    /// ou saut), la joueuse est donc gardée « en l'air » comme par <c>ForceJump</c>, sans son élan ni son bruit ;
    /// <c>m_jumpTimer</c> à 0 empêche aussi le contact au sol de se réinscrire (<c>OnCollisionStay</c>, 0,1 s).
    /// </summary>
    [HarmonyPatch]
    internal static class GrapplingAirbornePatch
    {
        private static GrapplingPoint s_pulling;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrapplingPoint), nameof(GrapplingPoint.Pull), new System.Type[0])]
        private static void AfterPull(GrapplingPoint __instance)
        {
            if (GrapplingConfig.ClickPull.Value && __instance == GrapplingPoint.m_localGrappler) s_pulling = __instance;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrapplingPoint), nameof(GrapplingPoint.Update), new System.Type[0])]
        private static void AfterUpdate(GrapplingPoint __instance)
        {
            if (__instance != s_pulling) return;
            Character character = __instance.m_character;
            if (__instance.m_secondary || character == null || !__instance.m_nview.IsOwner())
            {
                s_pulling = null;
                return;
            }
            character.ResetGroundContact();
            character.m_lastGroundTouch = 1f;
            character.m_jumpTimer = 0f;
        }
    }
}
