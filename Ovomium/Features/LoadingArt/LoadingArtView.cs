using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ovomium.Features.LoadingArt
{
    /// <summary>
    /// Mise en place d'une image de fond plein écran, entière et non déformée (bandes noires au besoin), insérée
    /// dans une hiérarchie d'écran de chargement vanilla : fond noir puis artwork à l'index demandé, les fonds unis
    /// vanilla du même parent (Image opaque sans sprite) désactivés pour ne pas recouvrir l'artwork.
    /// Tout ce qui est posé sur la scène est retrouvé par nom (jamais par type : rechargement à chaud) et mémorisé
    /// pour <see cref="Unload"/>.
    /// </summary>
    internal static class LoadingArtView
    {
        private const string BackdropName = "OvomiumLoadingBackdrop";
        private const string ArtName = "OvomiumLoadingArt";

        private static readonly List<Transform> s_parents = new List<Transform>();
        private static readonly List<Image> s_hidden = new List<Image>();

        /// <summary>
        /// Crée fond noir + artwork sous <paramref name="parent"/> à partir de <paramref name="siblingIndex"/>, ou
        /// réutilise ceux déjà en place (idempotent).
        /// </summary>
        internal static Image Install(Transform parent, int siblingIndex)
        {
            if (!s_parents.Contains(parent))
                s_parents.Add(parent);
            Image black = Find(parent, BackdropName) ?? CreateImage(parent, BackdropName);
            black.color = Color.black;
            black.transform.SetSiblingIndex(siblingIndex);
            Image art = Find(parent, ArtName) ?? CreateImage(parent, ArtName);
            art.transform.SetSiblingIndex(siblingIndex + 1);
            DisableSolidBackgrounds(parent);
            return art;
        }

        /// <summary>Affecte une image tirée au sort à <paramref name="image"/>, étirée à son parent.</summary>
        internal static void Apply(Image image)
        {
            Sprite sprite = LoadingArtLibrary.Next();
            if (sprite == null)
                return;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            Stretch(image.rectTransform);
        }

        /// <summary>Désactive une Image vanilla, mémorisée pour <see cref="Unload"/>.</summary>
        internal static void Hide(Image image)
        {
            if (image == null || !image.enabled)
                return;
            image.enabled = false;
            s_hidden.Add(image);
        }

        /// <summary>Désactive les Image de <paramref name="root"/> dont le sprite commence par <paramref name="spritePrefix"/>.</summary>
        internal static int HideSprites(Transform root, string spritePrefix)
        {
            int hidden = 0;
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                if (image.sprite != null && image.sprite.name.StartsWith(spritePrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    Hide(image);
                    hidden++;
                }
            return hidden;
        }

        /// <summary>
        /// Restaure la scène (déchargement du plugin) : nos objets détruits, les Image vanilla réactivées, la texture
        /// courante libérée. Les parents détruits entre-temps (retour au menu) sont ignorés.
        /// </summary>
        internal static void Unload()
        {
            foreach (Transform parent in s_parents)
                if (parent != null)
                    DestroyNamed(parent);
            foreach (Image image in s_hidden)
                if (image != null)
                    image.enabled = true;
            s_parents.Clear();
            s_hidden.Clear();
            LoadingArtLibrary.Release();
        }

        /// <summary>Diagnostic : enfants directs de chaque parent, avec leur Image éventuelle.</summary>
        internal static void LogHierarchy(params Transform[] parents)
        {
            foreach (Transform parent in parents)
            {
                if (parent == null)
                    continue;
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    Image image = child.GetComponent<Image>();
                    string tint = image == null ? "-" : $"{image.color} {(image.sprite == null ? "sans sprite" : image.sprite.name)}";
                    Plugin.Log.LogInfo($"LoadingArt :   {parent.name}[{i}] {child.name} actif={child.gameObject.activeSelf} image={tint}");
                }
            }
        }

        /// <summary>Fond uni vanilla : Image opaque sans sprite, enfant direct, autre que les nôtres.</summary>
        private static void DisableSolidBackgrounds(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == BackdropName || child.name == ArtName)
                    continue;
                Image image = child.GetComponent<Image>();
                if (image != null && image.enabled && image.sprite == null && image.color.a >= 0.99f)
                {
                    Hide(image);
                    Plugin.Log.LogInfo($"LoadingArt : fond uni vanilla désactivé : {parent.name}/{child.name}");
                }
            }
        }

        /// <summary>Enfant direct nommé, par nom (un composant posé par une autre assembly a un autre type).</summary>
        private static Image Find(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            return child == null ? null : child.GetComponent<Image>();
        }

        /// <summary>
        /// Détruit tous les enfants directs à nos noms (doublons d'anciens rechargements compris). Immédiat : la
        /// nouvelle assembly cherche par nom juste après, un objet en attente de destruction serait encore trouvé.
        /// </summary>
        private static void DestroyNamed(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name == BackdropName || child.name == ArtName)
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        private static Image CreateImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            Stretch(image.rectTransform);
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
