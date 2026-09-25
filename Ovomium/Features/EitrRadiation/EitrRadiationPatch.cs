using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.EitrRadiation
{
    /// <summary>
    /// La raffinerie d'Eitr en marche (enfant <c>_enabled</c>) et l'Eitr au sol (enfant <c>attach</c>) portent des
    /// <c>Radiator</c> : chez le propriétaire de leur ZDO, une boucle lance toutes les quelques secondes un projectile
    /// « radiation » (30 foudre + 30 poison, 20 m/s, 2 s de vie, sans attaquant) ; à l'impact, ce client appelle
    /// <c>Character.Damage</c> ou <c>WearNTear.Damage</c> (pièces construites), qui envoient <c>RPC_Damage</c> au
    /// propriétaire de la cible. Le coup est donc écarté aux deux bouts : à l'envoi (protège aussi les cibles des
    /// joueuses sans le mod quand l'émetteur a le mod) et à la réception (protège quand l'émetteur ne l'a pas). Arbres
    /// et roches : ni coupe ni pioche, le projectile ne leur fait rien. Il garde son vol et son effet d'impact.
    /// Signature d'un coup de radiation : pas d'attaquant, dégâts identiques à ceux du projectile d'un Radiator, point
    /// d'impact à portée de ce Radiator. Seuls ces deux prefabs portent un Radiator (bundle des prefabs, 1.0.15) ; l'Eitr
    /// posé sur un présentoir en porte aussi un (le présentoir instancie son enfant <c>attach</c>), d'où un filtre sur
    /// le Radiator et non sur le prefab hôte.
    /// </summary>
    internal static class EitrRadiationFilter
    {
        private const float RangeMargin = 5f;

        public static bool IsRadiationHit(HitData hit)
        {
            if (!EitrRadiationConfig.Enabled.Value || hit.HaveAttacker() || !hit.m_ranged)
                return false;
            // Inactifs compris : la raffinerie qui s'arrête désactive ses Radiator, ses projectiles volent encore 2 s.
            foreach (Radiator radiator in Object.FindObjectsByType<Radiator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (Matches(radiator, hit))
                    return true;
            }
            return false;
        }

        private static bool Matches(Radiator radiator, HitData hit)
        {
            Projectile projectile = radiator.m_projectile != null ? radiator.m_projectile.GetComponent<Projectile>() : null;
            if (projectile == null || !SameDamage(projectile.m_damage, hit.m_damage))
                return false;
            float range = radiator.m_velocity * projectile.m_ttl + RangeMargin;
            return (hit.m_point - radiator.transform.position).sqrMagnitude <= range * range;
        }

        private static bool SameDamage(HitData.DamageTypes a, HitData.DamageTypes b) =>
            a.m_damage == b.m_damage && a.m_blunt == b.m_blunt && a.m_slash == b.m_slash && a.m_pierce == b.m_pierce
            && a.m_chop == b.m_chop && a.m_pickaxe == b.m_pickaxe && a.m_fire == b.m_fire && a.m_frost == b.m_frost
            && a.m_lightning == b.m_lightning && a.m_poison == b.m_poison && a.m_spirit == b.m_spirit
            && a.m_nonPlayer == b.m_nonPlayer;
    }

    /// <summary>Côté émetteur : le coup n'est pas envoyé.</summary>
    [HarmonyPatch(typeof(Character), nameof(Character.Damage), new System.Type[] { typeof(HitData) })]
    internal static class EitrRadiationSendPatch
    {
        private static bool Prefix(HitData hit) => !EitrRadiationFilter.IsRadiationHit(hit);
    }

    /// <summary>Côté victime : le coup reçu est ignoré (ni dégâts, ni effet, ni chiffre).</summary>
    [HarmonyPatch(typeof(Character), "RPC_Damage", new System.Type[] { typeof(long), typeof(HitData) })]
    internal static class EitrRadiationReceivePatch
    {
        private static bool Prefix(HitData hit) => !EitrRadiationFilter.IsRadiationHit(hit);
    }

    /// <summary>Pièces construites, côté émetteur : même schéma que <c>Character</c> (<c>InvokeRPC</c> vers la propriétaire de la pièce).</summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Damage), new System.Type[] { typeof(HitData) })]
    internal static class EitrRadiationPieceSendPatch
    {
        private static bool Prefix(HitData hit) => !EitrRadiationFilter.IsRadiationHit(hit);
    }

    /// <summary>Pièces construites, côté propriétaire de la pièce : ni usure, ni chiffre.</summary>
    [HarmonyPatch(typeof(WearNTear), "RPC_Damage", new System.Type[] { typeof(long), typeof(HitData) })]
    internal static class EitrRadiationPieceReceivePatch
    {
        private static bool Prefix(HitData hit) => !EitrRadiationFilter.IsRadiationHit(hit);
    }
}
