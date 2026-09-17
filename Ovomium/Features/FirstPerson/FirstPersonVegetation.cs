using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.FirstPerson
{
    /// <summary>
    /// Les shaders de végétation du jeu effacent le feuillage près de la caméra (propriété <c>_CamCull</c>) pour ne
    /// pas boucher la vue à la troisième personne ; en vue subjective ce serait une bulle de vide autour du joueur.
    /// Mise à 0 sur tous les matériaux qui la portent (balayage unique à l'activation, puis les objets instanciés
    /// ensuite via <c>ZNetView.Awake</c>), valeurs d'origine restaurées à la sortie. Matériaux partagés : l'effet
    /// est global, ce qui est voulu.
    /// </summary>
    internal static class FirstPersonVegetation
    {
        private const string CamCull = "_CamCull";
        private static readonly Dictionary<Material, float> s_original = new Dictionary<Material, float>();

        internal static void Apply()
        {
            foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
                Apply(material);
        }

        internal static void Apply(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                foreach (Material material in renderer.sharedMaterials)
                    Apply(material);
        }

        internal static void Restore()
        {
            foreach (KeyValuePair<Material, float> entry in s_original)
                if (entry.Key != null)
                    entry.Key.SetFloat(CamCull, entry.Value);
            s_original.Clear();
        }

        private static void Apply(Material material)
        {
            if (material == null || s_original.ContainsKey(material) || !material.HasProperty(CamCull))
                return;
            s_original.Add(material, material.GetFloat(CamCull));
            material.SetFloat(CamCull, 0f);
        }
    }

    /// <summary>Objets chargés pendant la vue subjective (zones, instanciations).</summary>
    [HarmonyPatch(typeof(ZNetView), "Awake", new System.Type[0])]
    internal static class FirstPersonNewObjectPatch
    {
        private static void Postfix(ZNetView __instance)
        {
            if (FirstPersonMode.Active)
                FirstPersonVegetation.Apply(__instance.gameObject);
        }
    }
}
