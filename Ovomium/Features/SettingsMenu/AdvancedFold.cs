using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>
    /// Ligne cliquable « › Réglages avancés » qui affiche ou masque le groupe des options avancées d'une section
    /// (replié à la création, donc à chaque ouverture de la fenêtre). Le chevron est un « &gt; » tourné d'un quart de
    /// tour une fois déplié : présent dans toute police, contrairement aux triangles ▸ ▾. Gris au repos, couleur des
    /// libellés au survol ; inerte quand la section est désactivée (<see cref="CanvasGroup.interactable"/>).
    /// </summary>
    internal sealed class AdvancedFold : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const string Title = "Réglages avancés";
        private const float ArrowWidth = 14f;
        private static readonly Color s_idle = new Color(0.72f, 0.72f, 0.72f, 1f);

        private GameObject m_group;
        private CanvasGroup m_section;
        private TMP_Text m_label;
        private TMP_Text m_arrow;
        private Color m_hover;
        private bool m_open;

        /// <summary>
        /// Crée la ligne juste avant <paramref name="group"/> (même parent), et masque ce groupe : à appeler une fois
        /// ses lignes construites, pour qu'elles s'initialisent (Awake) pendant qu'il est encore actif.
        /// </summary>
        public static void Create(GameObject group, TMP_Text sample, float indent, CanvasGroup section)
        {
            var row = new GameObject(OvomiumSettingsWindow.NamePrefix + "AdvancedFold", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(group.transform.parent, false);
            row.transform.SetSiblingIndex(group.transform.GetSiblingIndex());
            row.GetComponent<Image>().color = Color.clear; // reçoit les clics sur toute la largeur de la ligne
            row.AddComponent<LayoutElement>().preferredHeight = SettingRows.RowHeight;
            var fold = row.AddComponent<AdvancedFold>();
            fold.m_group = group;
            fold.m_section = section;
            fold.m_hover = sample.color;
            fold.m_arrow = CreateText(sample, row.transform, ">", indent, ArrowWidth, TextAlignmentOptions.Center);
            fold.m_label = CreateText(sample, row.transform, Title, indent + ArrowWidth + 4f, 300f, TextAlignmentOptions.MidlineLeft);
            fold.SetOpen(false);
            fold.SetColor(s_idle);
        }

        private static TMP_Text CreateText(TMP_Text sample, Transform parent, string text, float x, float width,
            TextAlignmentOptions alignment)
        {
            var clone = Instantiate(sample, parent);
            clone.name = text == Title ? "Label" : "Arrow";
            clone.gameObject.SetActive(true);
            clone.text = text;
            clone.alignment = alignment;
            clone.raycastTarget = false;
            var rect = clone.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, 0f);
            rect.anchoredPosition = new Vector2(x + width / 2f, 0f);
            return clone;
        }

        private bool Interactable => m_section == null || m_section.interactable;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && Interactable)
                SetOpen(!m_open);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Interactable)
                SetColor(m_hover);
        }

        public void OnPointerExit(PointerEventData eventData) => SetColor(s_idle);

        private void SetOpen(bool open)
        {
            m_open = open;
            m_arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, open ? -90f : 0f);
            if (m_group == null)
                return;
            m_group.SetActive(open);
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }

        private void SetColor(Color color)
        {
            m_label.color = color;
            m_arrow.color = color;
        }
    }
}
