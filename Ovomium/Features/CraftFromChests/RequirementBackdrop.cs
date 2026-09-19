using System.Collections.Generic;
using Ovomium.Features.TooltipStyle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Fond noir semi-transparent arrondi derrière le nombre « requis (total) » d'une ligne d'ingrédient, pour rester
    /// lisible sur les icônes claires. Une <c>Image</c> sœur insérée juste avant le texte (un enfant se dessinerait
    /// par-dessus), redimensionnée à chaque passage sur la largeur préférée du texte (le rectangle vanilla est taillé
    /// pour un nombre seul, en débordement). Les objets créés vivent dans le prefab instancié : détruits par <see cref="Unload"/>.
    /// </summary>
    internal static class RequirementBackdrop
    {
        private const string Name = "OvomiumBackdrop";
        private const int Radius = 4;
        private const float PaddingY = 2f;
        private static readonly Vector3 Offset = new Vector3(0f, -2f, 0f);
        private static readonly Color Tint = new Color(0f, 0f, 0f, 0.68f);

        private static readonly List<GameObject> s_created = new List<GameObject>();

        /// <summary>Bande sur toute la largeur de la case, à la hauteur du texte rendu (indépendant du débordement).</summary>
        public static void Fit(TMP_Text text)
        {
            Image backdrop = Find(text.transform) ?? Create(text);
            text.ForceMeshUpdate();
            Bounds bounds = text.textBounds;
            var rect = backdrop.rectTransform;
            var cell = (RectTransform)text.rectTransform.parent;
            float y = text.rectTransform.localPosition.y + bounds.center.y + Offset.y;
            rect.localPosition = new Vector3(cell.rect.center.x, y, 0f);
            rect.sizeDelta = new Vector2(cell.rect.width, bounds.size.y + PaddingY * 2f);
        }

        private static Image Find(Transform text)
        {
            Transform parent = text.parent;
            int index = text.GetSiblingIndex();
            if (index == 0) return null;
            Transform previous = parent.GetChild(index - 1);
            return previous.name == Name ? previous.GetComponent<Image>() : null;
        }

        private static Image Create(TMP_Text text)
        {
            var go = new GameObject(Name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(text.transform.parent, false);
            rect.SetSiblingIndex(text.transform.GetSiblingIndex());
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = go.GetComponent<Image>();
            image.sprite = RoundedSprite.Get(Radius);
            image.type = Image.Type.Sliced;
            image.color = Tint;
            image.raycastTarget = false;
            s_created.Add(go);
            return image;
        }

        /// <summary>Rechargement à chaud : retire les fonds ajoutés aux lignes d'ingrédient.</summary>
        public static void Unload()
        {
            foreach (var go in s_created)
                if (go != null) Object.Destroy(go);
            s_created.Clear();
        }
    }
}
