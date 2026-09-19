using System.Collections.Generic;
using Ovomium.Features.CraftFromChests;
using UnityEngine;

namespace Ovomium.Features.ItemFlight
{
    /// <summary>Registre des vols en cours (plafond, annulation des vols d'un craft interrompu, déchargement à chaud).</summary>
    internal static class ItemFlight
    {
        private const float StaggerSeconds = 0.35f;

        private static readonly List<FlyingItem> s_flights = new List<FlyingItem>();
        private static readonly List<GameObject> s_lingering = new List<GameObject>();

        /// <summary>Un vol par retrait (un objet par type et par coffre), depuis le dessus du coffre.</summary>
        public static void Launch(List<Pull> pulls, Vector3 destination, bool fromCraft)
        {
            if (!ItemFlightConfig.Enabled.Value) return;
            var perChest = new Dictionary<Container, int>();
            foreach (var pull in pulls)
            {
                if (pull.Chest == null || pull.Item == null) continue;
                s_flights.RemoveAll(f => f == null);
                if (s_flights.Count >= ItemFlightConfig.MaxInFlight.Value) return;
                perChest.TryGetValue(pull.Chest, out int rank);
                perChest[pull.Chest] = rank + 1;
                Vector3 from = Front(pull.Chest.gameObject);
                s_flights.Add(Create(pull.Item, from, destination, rank * StaggerSeconds, fromCraft));
            }
        }

        private static FlyingItem Create(ItemDrop item, Vector3 from, Vector3 to, float delay, bool fromCraft)
        {
            var root = new GameObject("OvomiumFlight");
            GameObject mesh = FlightVisuals.CreateItem(item, root.transform);
            GameObject trail = FlightVisuals.CreateTrail(root.transform);
            float span = Mathf.Clamp01(Vector3.Distance(from, to) / CraftFromChestsConfig.Range.Value);
            float duration = Mathf.Lerp(ItemFlightConfig.MinDuration.Value, ItemFlightConfig.MaxDuration.Value, span);
            var flight = root.AddComponent<FlyingItem>();
            flight.FromCraft = fromCraft;
            flight.Setup(from, to, duration, delay, mesh.transform, trail);
            if (CraftFromChestsConfig.LogPulls.Value)
                Plugin.Log.LogInfo($"ItemFlight : {item.name} de {from} vers {to} en {duration:0.0} s, traînée {(trail != null ? "oui" : "non")}");
            return flight;
        }

        /// <summary>Le craft a consommé ses ingrédients : ses vols ne sont plus annulables.</summary>
        public static void CommitCraft()
        {
            foreach (var flight in s_flights)
                if (flight != null) flight.FromCraft = false;
        }

        /// <summary>Le craft a été interrompu : ses objets et leurs traînées disparaissent.</summary>
        public static void CancelCraft()
        {
            foreach (var flight in s_flights.ToArray())
                if (flight != null && flight.FromCraft) flight.Cancel();
        }

        /// <summary>Face avant du coffre (sens <c>forward</c> du prefab), au milieu, à 60 % de sa hauteur ; sinon sa position.</summary>
        public static Vector3 Front(GameObject target)
        {
            if (!FlightVisuals.TryGetBounds(target, out Bounds bounds)) return target.transform.position;
            Vector3 forward = target.transform.forward;
            float depth = (Mathf.Abs(forward.x) * bounds.size.x + Mathf.Abs(forward.z) * bounds.size.z) * 0.5f;
            return new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.6f, bounds.center.z) + forward * (depth + 0.1f);
        }

        /// <summary>Centre des rendus de l'objet (un chaudron pendu à son trépied : la marmite, pas le sommet), sinon sa position.</summary>
        public static Vector3 Center(GameObject target)
        {
            return FlightVisuals.TryGetBounds(target, out Bounds bounds) ? bounds.center : target.transform.position;
        }

        internal static void Linger(GameObject trail, float seconds)
        {
            s_lingering.RemoveAll(t => t == null);
            s_lingering.Add(trail);
            Object.Destroy(trail, seconds);
        }

        internal static void Forget(FlyingItem flight)
        {
            s_flights.Remove(flight);
        }

        public static void Unload()
        {
            foreach (var flight in s_flights.ToArray())
                if (flight != null) Object.Destroy(flight.gameObject);
            foreach (var trail in s_lingering)
                if (trail != null) Object.Destroy(trail);
            s_flights.Clear();
            s_lingering.Clear();
        }
    }
}
