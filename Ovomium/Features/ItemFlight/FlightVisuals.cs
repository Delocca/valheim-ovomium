using System.Collections.Generic;
using UnityEngine;

namespace Ovomium.Features.ItemFlight
{
    /// <summary>
    /// Copies purement visuelles des prefabs du jeu. Objet : enfant <c>attach</c> du prefab (version « en main »,
    /// meshes seuls) s'il existe, sinon le prefab entier instancié hors réseau (<c>ZNetView.m_forceDisableInit</c>,
    /// astuce des fantômes de construction) et réduit à ses rendus : sans <c>ItemDrop</c> ni physique, sinon
    /// l'auto-ramassage vanilla plante sur la copie (<c>Player.AutoPickup</c> déréférence son ZNetView).
    /// Traînée : les enfants porteurs de particules du prefab de projectile d'une flèche
    /// (<c>m_shared.m_attack.m_attackProjectile</c>), sans le projectile lui-même.
    /// </summary>
    internal static class FlightVisuals
    {
        private static readonly Dictionary<string, GameObject> s_trailPrefabs = new Dictionary<string, GameObject>();

        public static GameObject CreateItem(ItemDrop item, Transform parent)
        {
            Transform attach = item.transform.Find("attach");
            GameObject copy;
            if (attach != null)
                copy = Object.Instantiate(attach.gameObject, parent, false);
            else
            {
                copy = InstantiateOffline(item.gameObject, parent);
                StripToVisuals(copy);
            }
            copy.name = "OvomiumFlight_" + item.name;
            copy.transform.localPosition = Vector3.zero;
            CenterOnParent(copy);
            return copy;
        }

        /// <summary>Recentre les rendus de la copie sur son parent (le pivot d'un prefab est souvent à sa base).</summary>
        private static void CenterOnParent(GameObject copy)
        {
            if (!TryGetBounds(copy, out Bounds bounds)) return;
            copy.transform.position += copy.transform.parent.position - bounds.center;
        }

        public static GameObject CreateTrail(Transform parent)
        {
            GameObject prefab = TrailPrefab(ItemFlightConfig.TrailItem.Value);
            if (prefab == null) return null;
            var root = new GameObject("OvomiumTrail");
            root.transform.SetParent(parent, false);
            bool any = false;
            foreach (Transform child in prefab.transform)
            {
                if (child.GetComponentInChildren<ParticleSystem>(true) == null) continue;
                Object.Instantiate(child.gameObject, root.transform, false);
                any = true;
            }
            if (!any)
            {
                Plugin.Log.LogWarning($"ItemFlight : aucune particule dans le projectile de {ItemFlightConfig.TrailItem.Value}");
                s_trailPrefabs[ItemFlightConfig.TrailItem.Value] = null;
                Object.Destroy(root);
                return null;
            }
            StripToVisuals(root);
            TuneTrail(root);
            return root;
        }

        /// <summary>
        /// Ne garde que le système de particules nommé <c>TrailSystem</c> (la flèche de feu en a quatre : « trail »
        /// étincelles blanches, « flames », « flames_local », « flare »), sans le mesh de la flèche, converti en sillage :
        /// émis par mètre parcouru (<c>TrailDensity</c>), immobile là où il est émis (ni vitesse, ni montée, ni gravité,
        /// ni turbulence), vie <c>TrailLinger</c> secondes avec fondu progressif.
        /// </summary>
        private static void TuneTrail(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (!(renderer is ParticleSystemRenderer)) Object.DestroyImmediate(renderer);
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                Object.DestroyImmediate(filter);
            foreach (var system in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (system.name != ItemFlightConfig.TrailSystem.Value)
                {
                    Object.DestroyImmediate(system.GetComponent<ParticleSystemRenderer>());
                    Object.DestroyImmediate(system);
                    continue;
                }
                var emission = system.emission;
                emission.rateOverTime = 0f;
                emission.rateOverDistance = ItemFlightConfig.TrailDensity.Value;
                var main = system.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.gravityModifier = 0f;
                main.startSpeed = 0f;
                main.startLifetime = ItemFlightConfig.TrailLinger.Value;
                main.startSizeMultiplier *= ItemFlightConfig.TrailSize.Value;
                main.maxParticles = Mathf.Max(main.maxParticles, 5000);
                var shape = system.shape;
                shape.radius = ItemFlightConfig.TrailSpread.Value;
                shape.scale = Vector3.one;
                var velocity = system.velocityOverLifetime;
                velocity.enabled = false;
                var force = system.forceOverLifetime;
                force.enabled = false;
                var noise = system.noise;
                noise.enabled = false;
                Fade(system);
            }
            if (root.GetComponentInChildren<ParticleSystem>() == null)
                Plugin.Log.LogWarning($"ItemFlight : aucun système de particules nommé « {ItemFlightConfig.TrailSystem.Value} » (option TrailSystem)");
        }

        /// <summary>Opacité pleine sur le premier tiers de vie, puis extinction progressive.</summary>
        private static void Fade(ParticleSystem system)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.33f), new GradientAlphaKey(0f, 1f) });
            var color = system.colorOverLifetime;
            color.color = gradient;
            color.enabled = true;
        }

        /// <summary>Arrête l'émission (les particules déjà émises finissent leur vie) : recette de <c>Character.SetupContinuousEffect</c>.</summary>
        public static void StopEmission(GameObject trail)
        {
            foreach (var system in trail.GetComponentsInChildren<ParticleSystem>())
            {
                var emission = system.emission;
                emission.enabled = false;
                system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        public static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer) continue;
                if (any) bounds.Encapsulate(renderer.bounds);
                else { bounds = renderer.bounds; any = true; }
            }
            return any;
        }

        private static GameObject TrailPrefab(string itemName)
        {
            if (s_trailPrefabs.TryGetValue(itemName, out GameObject cached)) return cached;
            GameObject prefab = null;
            GameObject item = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(itemName) : null;
            var drop = item != null ? item.GetComponent<ItemDrop>() : null;
            if (drop != null) prefab = drop.m_itemData.m_shared.m_attack.m_attackProjectile;
            if (prefab == null)
                Plugin.Log.LogWarning($"ItemFlight : pas de projectile pour « {itemName} » (option TrailItem), vol sans traînée");
            s_trailPrefabs[itemName] = prefab;
            return prefab;
        }

        private static GameObject InstantiateOffline(GameObject prefab, Transform parent)
        {
            ZNetView.m_forceDisableInit = true;
            try { return Object.Instantiate(prefab, parent, false); }
            finally { ZNetView.m_forceDisableInit = false; }
        }

        /// <summary>Ne garde que rendus, particules et lumières : scripts d'abord (certains exigent le Rigidbody), puis physique.</summary>
        private static void StripToVisuals(GameObject root)
        {
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                Object.DestroyImmediate(behaviour);
            foreach (var audio in root.GetComponentsInChildren<AudioSource>(true))
                Object.DestroyImmediate(audio);
            foreach (var joint in root.GetComponentsInChildren<Joint>(true))
                Object.DestroyImmediate(joint);
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            foreach (var body in root.GetComponentsInChildren<Rigidbody>(true))
                Object.DestroyImmediate(body);
        }
    }
}
