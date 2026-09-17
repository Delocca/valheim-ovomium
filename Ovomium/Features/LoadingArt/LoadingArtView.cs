using UnityEngine;
using UnityEngine.UI;

namespace Ovomium.Features.LoadingArt
{
    /// <summary>
    /// Mise en place d'une image de fond plein écran, entière et non déformée (bandes noires au besoin), insérée
    /// dans une hiérarchie d'écran de chargement vanilla : fond noir puis artwork à l'index demandé, les fonds unis
    /// vanilla du même parent (Image opaque sans sprite) désactivés pour ne pas recouvrir l'artwork.
    /// </summary>
    internal static class LoadingArtView
    {
        private const string BackdropName = "OvomiumLoadingBackdrop";
        private const string ArtName = "OvomiumLoadingArt";

        /// <summary>Crée fond noir + artwork sous <paramref name="parent"/> à partir de <paramref name="siblingIndex"/>.</summary>
        internal static Image Install(Transform parent, int siblingIndex)
        {
            Image black = CreateImage(parent, BackdropName);
            black.color = Color.black;
            black.transform.SetSiblingIndex(siblingIndex);
            Image art = CreateImage(parent, ArtName);
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

        /// <summary>Désactive les Image de <paramref name="root"/> dont le sprite commence par <paramref name="spritePrefix"/>.</summary>
        internal static int HideSprites(Transform root, string spritePrefix)
        {
            int hidden = 0;
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                if (image.sprite != null && image.sprite.name.StartsWith(spritePrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    image.enabled = false;
                    hidden++;
                }
            return hidden;
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
                    image.enabled = false;
                    Plugin.Log.LogInfo($"LoadingArt : fond uni vanilla désactivé : {parent.name}/{child.name}");
                }
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
