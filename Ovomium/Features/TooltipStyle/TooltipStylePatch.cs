using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ovomium.Features.TooltipStyle
{
    /// <summary>
    /// <c>UITooltip.OnHoverStart(GameObject)</c> instancie l'infobulle (<c>UITooltip.m_tooltip</c>, privé statique, une
    /// seule à la fois, détruite par <c>HideTooltip</c>) à partir du prefab du composant survolé. Le postfix stylise
    /// l'instance (jamais le prefab : rien à restaurer) : opacité et sprite arrondi sur l'<c>Image</c> de fond, si le
    /// prefab en a une. Un composant marqueur évite de retraiter la même instance à chaque survol.
    /// </summary>
    [HarmonyPatch(typeof(UITooltip), "OnHoverStart", new System.Type[] { typeof(GameObject) })]
    internal static class TooltipStylePatch
    {
        private static readonly System.Reflection.FieldInfo s_tooltipField = AccessTools.Field(typeof(UITooltip), "m_tooltip");
        private const int BorderWidth = 1;
        /// <summary>Beige clair, dans les tons du titre orangé des infobulles.</summary>
        private static readonly Color BorderTint = new Color(0.90f, 0.84f, 0.72f);
        /// <summary>Marron sombre du fond (choix d'Edia), à la place du noir vanilla.</summary>
        private static readonly Color BackgroundTint = new Color(0.16f, 0.11f, 0.07f);

        /// <summary>Marque une instance déjà stylisée.</summary>
        private sealed class Styled : MonoBehaviour { }

        private static void Postfix()
        {
            var tooltip = s_tooltipField.GetValue(null) as GameObject;
            if (tooltip == null || tooltip.GetComponent<Styled>() != null)
                return;
            tooltip.AddComponent<Styled>();
            if (TooltipStyleConfig.LogHierarchy.Value)
                Dump(tooltip.transform);
            if (!TooltipStyleConfig.Enabled.Value)
                return;
            Image background = FindBackground(tooltip.transform);
            if (background != null)
                Style(background);
        }

        /// <summary>Rechargement à chaud : ferme l'infobulle en cours (elle porte le sprite du mod) et libère le sprite.</summary>
        internal static void Unload()
        {
            UITooltip.HideTooltip();
            RoundedSprite.Release();
        }

        /// <summary>Première Image sous le rectangle cadré (enfant 0) qui ne porte pas de texte : le fond, ou null.</summary>
        private static Image FindBackground(Transform root)
        {
            if (root.childCount == 0)
                return null;
            foreach (Image image in root.GetChild(0).GetComponentsInChildren<Image>(true))
                if (image.GetComponent<TMP_Text>() == null)
                    return image;
            return null;
        }

        /// <summary>
        /// Fond vanilla : sprite « Background » (semi-transparent) teinté noir à a = 0,95. Le sprite est remplacé par
        /// une forme pleine teintée marron sombre, à l'opacité voulue.
        /// </summary>
        private static void Style(Image background)
        {
            background.color = new Color(BackgroundTint.r, BackgroundTint.g, BackgroundTint.b,
                TooltipStyleConfig.BackgroundOpacity.Value);
            int radius = TooltipStyleConfig.CornerRadius.Value;
            if (radius <= 0)
                return;
            background.sprite = RoundedSprite.Get(radius);
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 1f;
            if (TooltipStyleConfig.BorderOpacity.Value > 0f)
                AddBorder(background, radius);
        }

        /// <summary>
        /// Liseré : Image enfant du fond, même rectangle, anneau arrondi de 1 px ; premier enfant pour rester sous
        /// les textes, hors layout (le fond porte un VerticalLayoutGroup).
        /// </summary>
        private static void AddBorder(Image background, int radius)
        {
            var go = new GameObject("OvomiumBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            var rect = (RectTransform)go.transform;
            rect.SetParent(background.transform, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            Image image = go.GetComponent<Image>();
            image.sprite = RoundedSprite.Get(radius, BorderWidth);
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            image.color = new Color(BorderTint.r, BorderTint.g, BorderTint.b, TooltipStyleConfig.BorderOpacity.Value);
        }

        private static void Dump(Transform root)
        {
            var sb = new StringBuilder();
            sb.Append("TooltipStyle : hiérarchie de ").Append(root.name).AppendLine(" :");
            Append(sb, root, 0);
            Plugin.Log.LogInfo(sb.ToString());
        }

        private static void Append(StringBuilder sb, Transform t, int depth)
        {
            sb.Append(' ', depth * 2).Append(t.name);
            if (!t.gameObject.activeSelf)
                sb.Append(" (inactif)");
            if (t is RectTransform rect)
                sb.Append(" sizeDelta=").Append(rect.sizeDelta.ToString("F0")).Append(" rect=").Append(rect.rect.size.ToString("F0"));
            foreach (Image image in t.GetComponents<Image>())
                sb.Append(" Image{sprite=").Append(image.sprite != null ? image.sprite.name : "null")
                  .Append(" color=").Append(image.color.ToString("F2")).Append(" type=").Append(image.type)
                  .Append(" material=").Append(image.material != null ? image.material.name : "null").Append('}');
            if (t.GetComponent<TMP_Text>() is TMP_Text text)
                sb.Append(" TMP_Text{color=").Append(text.color.ToString("F2")).Append(" taille=").Append(text.fontSize).Append('}');
            foreach (Component c in t.GetComponents<Component>())
                if (c != null && !(c is Transform) && !(c is Image) && !(c is TMP_Text))
                    sb.Append(' ').Append(c.GetType().Name);
            sb.AppendLine();
            for (int i = 0; i < t.childCount; i++)
                Append(sb, t.GetChild(i), depth + 1);
        }
    }
}
