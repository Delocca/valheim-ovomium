using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ovomium.Features.PasswordReveal
{
    /// <summary>
    /// Case à cocher « Mémoriser » sous le champ mot de passe (enfant du champ, comme le bouton Afficher / Masquer,
    /// et pour la même raison pas un Toggle Unity : un Selectable volerait le focus du champ). Carré au style du
    /// champ, coche pleine quand actif, libellé à droite. L'état est lu par <see cref="PasswordRevealPatch"/>
    /// à la soumission, après la fermeture du dialogue : d'où le champ statique <see cref="Checked"/>.
    /// </summary>
    internal sealed class PasswordRememberToggle : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const string ObjectName = "Ovomium_PasswordRemember";
        private const float Height = 22f;
        private const float Gap = 6f;
        private const float BoxSize = 18f;
        private const float MarkSize = 10f;
        private const float LabelWidth = 200f;
        private static readonly Color s_idle = new Color(1f, 1f, 1f, 0.75f);

        /// <summary>État de la case, valable après <see cref="Setup"/> jusqu'à la prochaine ouverture du dialogue.</summary>
        internal static bool Checked { get; private set; }

        private Image m_mark;
        private TMP_Text m_label;

        /// <summary>Crée la case s'il le faut, puis la met dans l'état demandé.</summary>
        internal static void Setup(TMP_InputField field, bool isChecked)
        {
            Transform existing = field.transform.Find(ObjectName);
            PasswordRememberToggle toggle = existing != null
                ? existing.GetComponent<PasswordRememberToggle>()
                : Build(field);
            Checked = isChecked;
            toggle.Apply();
        }

        private static PasswordRememberToggle Build(TMP_InputField field)
        {
            GameObject go = new GameObject(ObjectName, typeof(RectTransform), typeof(Image), typeof(PasswordRememberToggle));
            go.transform.SetParent(field.transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -Gap);
            rect.sizeDelta = new Vector2(BoxSize + Gap + LabelWidth, Height);
            Image hit = go.GetComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;

            PasswordRememberToggle toggle = go.GetComponent<PasswordRememberToggle>();
            RectTransform box = BuildBox(go.transform, field.GetComponent<Image>());
            toggle.m_mark = BuildMark(box, field.textComponent);
            toggle.m_label = BuildLabel(go.transform, field.textComponent);
            return toggle;
        }

        private static RectTransform BuildBox(Transform parent, Image reference)
        {
            GameObject go = new GameObject("box", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(BoxSize, BoxSize);
            PasswordRevealToggle.CopyBackground(reference, go.GetComponent<Image>());
            go.GetComponent<Image>().raycastTarget = false;
            return rect;
        }

        private static Image BuildMark(Transform parent, TMP_Text reference)
        {
            GameObject go = new GameObject("mark", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(MarkSize, MarkSize);
            Image mark = go.GetComponent<Image>();
            mark.color = reference != null ? reference.color : Color.white;
            mark.raycastTarget = false;
            return mark;
        }

        private static TMP_Text BuildLabel(Transform parent, TMP_Text reference)
        {
            TMP_Text label = PasswordRevealToggle.BuildLabel(parent, reference);
            RectTransform rect = label.rectTransform;
            rect.offsetMin = new Vector2(BoxSize + Gap, 0f);
            label.alignment = TextAlignmentOptions.Left;
            return label;
        }

        private void Apply()
        {
            m_mark.enabled = Checked;
            m_label.text = PasswordRevealConfig.RememberLabel.Value;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            Checked = !Checked;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_label.color = new Color(m_label.color.r, m_label.color.g, m_label.color.b, 1f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_label.color = new Color(m_label.color.r, m_label.color.g, m_label.color.b, s_idle.a);
        }
    }
}
