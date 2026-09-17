using System.Collections.Generic;
using UnityEngine;

namespace OvoMiam.Features.FastPortal
{
    /// <summary>
    /// Condition d'arrivée plus stricte que ZNetScene.IsAreaReady (qui ne regarde que la zone cible et ses objets
    /// proches) : toutes les zones de l'aire active autour du point sont chargées, et tous leurs objets, proches
    /// comme lointains, sont instanciés.
    /// </summary>
    internal static class FastPortalArrival
    {
        private static readonly List<ZDO> s_near = new List<ZDO>();
        private static readonly List<ZDO> s_far = new List<ZDO>();

        internal static bool IsFullyLoaded(Vector3 point)
        {
            ZoneSystem zones = ZoneSystem.instance;
            SimulationDistance distance = zones.m_simulationDistance;
            Vector2s center = ZoneSystem.GetZone(point);
            int radius = distance.NearSimulationDistance;
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                for (int x = center.x - radius; x <= center.x + radius; x++)
                {
                    Vector2s zone = new Vector2s(x, y);
                    bool inArea = distance.IsClassic || zones.ZonesWithinRadius(center, zone, radius);
                    if (inArea && !zones.IsZoneLoaded(zone))
                        return false;
                }
            }
            s_near.Clear();
            s_far.Clear();
            ZDOMan.instance.FindSectorObjects(center, distance, s_near, s_far);
            return AllInstantiated(s_near) && AllInstantiated(s_far);
        }

        private static bool AllInstantiated(List<ZDO> zdos)
        {
            ZNetScene scene = ZNetScene.instance;
            foreach (ZDO zdo in zdos)
            {
                if (scene.IsPrefabZDOValid(zdo) && !scene.FindInstance(zdo))
                    return false;
            }
            return true;
        }
    }
}
