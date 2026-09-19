using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.ButcherKnife
{
    /// <summary>
    /// Arme réservée aux apprivoisés (<c>m_tamedOnly</c>, couteau de boucher) : <c>DoMeleeAttack</c> balaie un cône
    /// et accumule ses cibles par <c>AddHitPoint</c> (seul appelant) ; on y refuse toute cible autre que la créature
    /// visée par le joueur local (<c>Player.GetHoverCreature</c>). <c>go</c> vient de <c>Projectile.FindHitObject</c>
    /// (objet porteur de l'IDestructible, donc du Character).
    /// </summary>
    [HarmonyPatch(typeof(Attack), "AddHitPoint", new System.Type[] { typeof(List<Attack.HitPoint>), typeof(GameObject), typeof(Collider), typeof(Vector3), typeof(float), typeof(bool) })]
    internal static class ButcherKnifePatch
    {
        private static bool Prefix(Attack __instance, GameObject go)
        {
            if (!ButcherKnifeConfig.Enabled.Value)
                return true;
            Player player = Player.m_localPlayer;
            if (player == null || __instance.m_character != player || __instance.m_weapon == null || !__instance.m_weapon.m_shared.m_tamedOnly)
                return true;
            Character target = player.GetHoverCreature();
            return target != null && go.GetComponent<Character>() == target;
        }
    }
}
