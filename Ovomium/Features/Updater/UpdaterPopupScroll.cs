using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Fait défiler le corps de la fenêtre quand il dépasse la hauteur permise : <c>BodyText</c> est déplacé dans
    /// Scroll (ScrollRect, à sa place et à sa taille) → Viewport (masque, image transparente qui reçoit molette et
    /// glisser) ; barre fine déplaçable à droite (souris sans molette, pointeur VR). Vanilla ne touche que le texte et
    /// l'alignement du corps (<c>UnifiedPopup.ResetUI</c>), jamais sa place : tout est remis par <see cref="Unwrap"/>.
    /// </summary>
    internal static class UpdaterPopupScroll
    {
        private const float BarWidth = 8f;
        private const float BarSpacing = 6f;
        private const float Sensitivity = 300f;  // une dizaine de lignes par cran (la fenêtre Ovomium : 600, vingt)
        private static readonly Color TrackColor = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color HandleColor = new Color(0.91f, 0.84f, 0.66f, 0.85f);  // #E8D5A8 du mod

        private static GameObject s_root;
        private static TextMeshProUGUI s_body;
        private static Transform s_parent;
        private static int s_siblingIndex;
        private static Vector2 s_anchorMin, s_anchorMax, s_pivot, s_position;

        public static bool IsWrapped => s_root != null && s_body != null;

        /// <summary>Corps déjà à sa taille visible (sizeDelta posé) ; le texte repart en haut.</summary>
        public static void Wrap(TextMeshProUGUI body)
        {
            Unwrap();
            RectTransform bodyRect = body.rectTransform;
            s_body = body;
            s_parent = bodyRect.parent;
            s_siblingIndex = bodyRect.GetSiblingIndex();
            s_anchorMin = bodyRect.anchorMin;
            s_anchorMax = bodyRect.anchorMax;
            s_pivot = bodyRect.pivot;
            s_position = bodyRect.anchoredPosition;

            s_root = new GameObject("OvomiumScroll", typeof(RectTransform));
            var rootRect = (RectTransform)s_root.transform;
            rootRect.SetParent(s_parent, false);
            rootRect.SetSiblingIndex(s_siblingIndex);
            rootRect.anchorMin = s_anchorMin;
            rootRect.anchorMax = s_anchorMax;
            rootRect.pivot = s_pivot;
            rootRect.anchoredPosition = s_position;
            rootRect.sizeDelta = bodyRect.sizeDelta;

            RectTransform viewport = BuildViewport(rootRect);
            bodyRect.SetParent(viewport, false);
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = Vector2.one;
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = Vector2.zero;
            Refit();

            var scroll = s_root.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = bodyRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = Sensitivity;
            scroll.verticalScrollbar = BuildScrollbar(rootRect);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        /// <summary>Hauteur du contenu = hauteur du texte ; à rappeler quand le texte change (progression).</summary>
        public static void Refit()
        {
            if (!IsWrapped)
                return;
            RectTransform bodyRect = s_body.rectTransform;
            float height = s_body.GetPreferredValues(bodyRect.rect.width, 0f).y;
            bodyRect.sizeDelta = new Vector2(0f, height);
        }

        /// <summary>Remet le corps à sa place d'origine (taille : <see cref="UpdaterPopupLayout"/>) ; sans effet sinon.</summary>
        public static void Unwrap()
        {
            if (s_body != null && s_parent != null)
            {
                RectTransform bodyRect = s_body.rectTransform;
                bodyRect.SetParent(s_parent, false);
                bodyRect.SetSiblingIndex(s_siblingIndex);
                bodyRect.anchorMin = s_anchorMin;
                bodyRect.anchorMax = s_anchorMax;
                bodyRect.pivot = s_pivot;
                bodyRect.anchoredPosition = s_position;
            }
            if (s_root != null)
            {
                s_root.SetActive(false);  // Destroy est différé : le ScrollRect ne doit plus bouger le corps d'ici là
                Object.Destroy(s_root);
            }
            s_root = null;
            s_body = null;
            s_parent = null;
        }

        private static RectTransform BuildViewport(RectTransform root)
        {
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.GetComponent<Image>().color = Color.clear;
            var rect = (RectTransform)viewport.transform;
            rect.SetParent(root, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(-(BarWidth + BarSpacing), 0f);
            return rect;
        }

        private static Scrollbar BuildScrollbar(RectTransform root)
        {
            var bar = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            bar.GetComponent<Image>().color = TrackColor;
            var rect = (RectTransform)bar.transform;
            rect.SetParent(root, false);
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(BarWidth, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = HandleColor;
            var handleRect = (RectTransform)handle.transform;
            handleRect.SetParent(rect, false);
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            var scrollbar = bar.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };  // la manette reste sur les boutons
            return scrollbar;
        }
    }
}
