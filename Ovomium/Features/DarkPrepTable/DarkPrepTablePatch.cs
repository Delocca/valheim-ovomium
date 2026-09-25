using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.DarkPrepTable
{
    /// <summary>
    /// Assombrit la table de préparation (prefab <c>piece_preptable</c>, station <c>$piece_preptable</c>) : chaque
    /// matériau de ses renderers (enfants inactifs compris : états usé / cassé, LOD) est remplacé par un clone dont la
    /// texture est une copie de l'atlas au bois assombri (<see cref="WoodTexture"/> : table et décor partagent mesh,
    /// matériau et atlas, une teinte <c>_Color</c> assombrirait aussi le décor). Les matériaux et textures d'origine,
    /// qui peuvent servir à d'autres objets (table décorative des halls du Grand Nord), ne sont jamais modifiés. Appliqué au prefab dès que
    /// <c>ZNetScene</c> existe (les tables posées ensuite en héritent, le fantôme de placement copie les clones) et
    /// aux tables déjà en scène. La surbrillance du marteau (<c>MaterialMan</c>, property block posé puis vidé)
    /// passe par-dessus sans toucher aux matériaux.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), "Awake", new System.Type[0])]
    internal static class DarkPrepTablePatch
    {
        private const string PrefabName = "piece_preptable";

        /// <summary>Matériau d'origine → clone assombri, et l'inverse pour la restauration.</summary>
        private static readonly Dictionary<Material, Material> s_clones = new Dictionary<Material, Material>();
        private static readonly Dictionary<Material, Material> s_originals = new Dictionary<Material, Material>();
        /// <summary>Atlas d'origine → copie au bois assombri (partagée par les matériaux qui l'utilisent).</summary>
        private static readonly Dictionary<Texture2D, WoodTexture> s_woods = new Dictionary<Texture2D, WoodTexture>();
        /// <summary>Gardé hors de ZNetScene : au menu (scène de jeu déchargée), l'Unload doit encore le restaurer.</summary>
        private static GameObject s_prefab;

        private static void Postfix(ZNetScene __instance)
        {
            s_prefab = __instance.GetPrefab(PrefabName);
            Refresh();
        }

        /// <summary>Rechargement à chaud en partie : prefab et tables déjà en scène, et suivi des réglages.</summary>
        internal static void Install()
        {
            DarkPrepTableConfig.Enabled.SettingChanged += (_, __) => Refresh();
            DarkPrepTableConfig.Brightness.SettingChanged += (_, __) => UpdateTextures();
            if (ZNetScene.instance != null)
                s_prefab = ZNetScene.instance.GetPrefab(PrefabName);
            Refresh();
        }

        internal static void Unload() => Restore();

        private static void Refresh()
        {
            if (DarkPrepTableConfig.Enabled.Value)
                Apply();
            else
                Restore();
        }

        private static void Apply()
        {
            if (s_prefab == null)
                return;
            int known = s_clones.Count;
            SwapMaterials(s_prefab, ToClone);
            IEnumerable<GameObject> tables = LiveTables();
            foreach (GameObject table in tables)
                SwapMaterials(table, ToClone);
            UpdateTextures();
            if (s_clones.Count > known)
                Plugin.Log.LogInfo("DarkPrepTable : matériaux assombris : " + string.Join(", ",
                    s_clones.Keys.Skip(known).Select(m => $"{m.name} ({m.shader.name})")) +
                    $" ; {tables.Count()} table(s) déjà en scène");
        }

        private static void Restore()
        {
            if (s_clones.Count > 0)
            {
                if (s_prefab != null)
                    SwapMaterials(s_prefab, ToOriginal);
                foreach (GameObject table in LiveTables())
                    SwapMaterials(table, ToOriginal);
            }
            foreach (Material clone in s_originals.Keys)
                Object.Destroy(clone);
            foreach (WoodTexture wood in s_woods.Values)
                wood?.Destroy();
            s_clones.Clear();
            s_originals.Clear();
            s_woods.Clear();
        }

        private static void UpdateTextures()
        {
            foreach (WoodTexture wood in s_woods.Values)
                wood?.Apply(DarkPrepTableConfig.Brightness.Value);
        }

        /// <summary>Tables en scène (le fantôme de placement en est une, mais ses matériaux sont ses propres copies).</summary>
        private static IEnumerable<GameObject> LiveTables()
            => Piece.s_allPieces.Where(p => p != null && Utils.GetPrefabName(p.gameObject) == PrefabName)
                .Select(p => p.gameObject).ToList();

        /// <summary>Remplace les matériaux des MeshRenderer / SkinnedMeshRenderer (ceux que gère MaterialMan).</summary>
        private static void SwapMaterials(GameObject root, System.Func<Material, Material> map)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                    continue;
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material swapped = map(materials[i]);
                    changed |= swapped != materials[i];
                    materials[i] = swapped;
                }
                if (changed)
                    renderer.sharedMaterials = materials;
            }
        }

        /// <summary>Sans atlas reconnu (couche de neige <c>Valheim/Snow Mesh</c>, atlas changé), le matériau reste
        /// partagé.</summary>
        private static Material ToClone(Material material)
        {
            if (material == null || s_originals.ContainsKey(material))
                return material;
            if (s_clones.TryGetValue(material, out Material clone))
                return clone;
            WoodTexture wood = WoodFor(material);
            if (wood == null)
                return material;
            clone = new Material(material) { name = material.name + " (Ovomium sombre)", mainTexture = wood.Copy };
            s_clones[material] = clone;
            s_originals[clone] = material;
            return clone;
        }

        /// <summary>Copie au bois assombri de l'atlas du matériau, créée une fois (null mémorisé si non reconnu).</summary>
        private static WoodTexture WoodFor(Material material)
        {
            if (!material.HasProperty("_MainTex") || !(material.mainTexture is Texture2D atlas))
                return null;
            if (!s_woods.TryGetValue(atlas, out WoodTexture wood))
                s_woods[atlas] = wood = WoodTexture.Create(atlas);
            return wood;
        }

        private static Material ToOriginal(Material material)
            => material != null && s_originals.TryGetValue(material, out Material original) ? original : material;
    }
}
