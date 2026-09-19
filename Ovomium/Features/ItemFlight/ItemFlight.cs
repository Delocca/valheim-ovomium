using System.Collections.Generic;
using Ovomium.Features.CraftFromChests;
using UnityEngine;

namespace Ovomium.Features.ItemFlight
{
    /// <summary>Registre des vols en cours (plafond, annulation des vols d'un craft interrompu, déchargement à chaud).</summary>
    internal static class ItemFlight
    {
        /// <summary>Écart entre deux exemplaires d'un même ingrédient (ils se suivent en file).</summary>
        private const float TrainSeconds = 0.15f;
        /// <summary>Écart supplémentaire entre deux ingrédients différents partant du même coffre.</summary>
        private const float StaggerSeconds = 0.35f;

        private static readonly List<FlyingItem> s_flights = new List<FlyingItem>();
        private static readonly List<GameObject> s_lingering = new List<GameObject>();

        /// <summary>
        /// Envoie les objets retirés depuis la face avant de leur coffre : plusieurs exemplaires par type
        /// (<see cref="ItemFlightConfig.MaxPerType"/>), en file sur la même trajectoire, dont seul le premier
        /// porte une traînée. Créés rang par rang (le premier de chaque retrait, puis les deuxièmes, etc.) :
        /// si <see cref="ItemFlightConfig.MaxInFlight"/> sature, ce sont des doublons qui manquent, jamais un type.
        /// </summary>
        public static void Launch(List<Pull> pulls, Vector3 destination, bool fromCraft)
        {
            if (!ItemFlightConfig.Enabled.Value) return;
            int[] counts = Share(pulls);
            var starts = new float[pulls.Count];
            var phases = new float[pulls.Count];
            var perChest = new Dictionary<Container, float>();
            int ranks = 0;
            for (int i = 0; i < pulls.Count; i++)
            {
                if (counts[i] <= 0) continue;
                perChest.TryGetValue(pulls[i].Chest, out starts[i]);
                perChest[pulls[i].Chest] = starts[i] + counts[i] * TrainSeconds + StaggerSeconds;
                phases[i] = Random.value * Mathf.PI * 2f;
                ranks = Mathf.Max(ranks, counts[i]);
                if (CraftFromChestsConfig.LogPulls.Value)
                    Plugin.Log.LogInfo($"ItemFlight : {counts[i]} × {pulls[i].Item.name} de {pulls[i].Chest.m_name} vers {destination}");
            }
            for (int n = 0; n < ranks; n++)
                for (int i = 0; i < pulls.Count; i++)
                {
                    if (counts[i] <= n) continue;
                    s_flights.RemoveAll(f => f == null);
                    if (s_flights.Count >= ItemFlightConfig.MaxInFlight.Value) return;
                    Vector3 from = Front(pulls[i].Chest.gameObject);
                    float delay = starts[i] + n * TrainSeconds;
                    s_flights.Add(Create(pulls[i].Item, from, destination, delay, phases[i], n == 0, fromCraft));
                }
        }

        /// <summary>
        /// Nombre d'exemplaires animés pour chaque retrait : au plus <see cref="ItemFlightConfig.MaxPerType"/> par
        /// type d'objet, d'abord un par coffre qui y contribue (on voit tous les coffres puiser), le reste distribué
        /// à tour de rôle sans dépasser ce que chacun fournit.
        /// </summary>
        private static int[] Share(List<Pull> pulls)
        {
            var counts = new int[pulls.Count];
            var byItem = new Dictionary<string, List<int>>();
            for (int i = 0; i < pulls.Count; i++)
            {
                if (pulls[i].Chest == null || pulls[i].Item == null || pulls[i].Amount <= 0) continue;
                string name = pulls[i].Item.m_itemData.m_shared.m_name;
                if (!byItem.TryGetValue(name, out List<int> indexes))
                    byItem[name] = indexes = new List<int>();
                indexes.Add(i);
            }
            foreach (List<int> indexes in byItem.Values)
            {
                int budget = ItemFlightConfig.MaxPerType.Value;
                foreach (int i in indexes)
                    if (budget > 0) { counts[i] = 1; budget--; }
                bool grew = true;
                while (budget > 0 && grew)
                {
                    grew = false;
                    foreach (int i in indexes)
                    {
                        if (budget <= 0) break;
                        if (counts[i] <= 0 || counts[i] >= pulls[i].Amount) continue;
                        counts[i]++;
                        budget--;
                        grew = true;
                    }
                }
            }
            return counts;
        }

        private static FlyingItem Create(ItemDrop item, Vector3 from, Vector3 to, float delay, float phase, bool withTrail, bool fromCraft)
        {
            var root = new GameObject("OvomiumFlight");
            GameObject mesh = FlightVisuals.CreateItem(item, root.transform);
            GameObject trail = withTrail ? FlightVisuals.CreateTrail(root.transform) : null;
            float span = Mathf.Clamp01(Vector3.Distance(from, to) / CraftFromChestsConfig.Range.Value);
            float duration = Mathf.Lerp(ItemFlightConfig.MinDuration.Value, ItemFlightConfig.MaxDuration.Value, span);
            var flight = root.AddComponent<FlyingItem>();
            flight.FromCraft = fromCraft;
            flight.Setup(from, to, duration, delay, mesh.transform, trail, phase);
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
