using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.MapExplore
{
    /// <summary>
    /// Rayon de découverte de la carte multiplié. <c>Minimap.Explore(Vector3, float)</c> n'a qu'un appelant,
    /// <c>UpdateExplore</c>, qui lui passe <c>m_exploreRadius</c> : on multiplie l'argument à l'entrée.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Explore), new System.Type[] { typeof(Vector3), typeof(float) })]
    internal static class MapExplorePatch
    {
        private static void Prefix(ref float radius)
        {
            if (!MapExploreConfig.Enabled.Value)
                return;
            float factor = Ship.GetLocalShip() != null ? MapExploreConfig.BoatFactor.Value : MapExploreConfig.LandFactor.Value;
            radius *= factor;
        }
    }
}
