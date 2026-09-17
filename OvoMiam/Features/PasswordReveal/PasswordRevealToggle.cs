using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OvoMiam.Features.PasswordReveal
{
    /// <summary>
    /// Bouton « Afficher / Masquer » posé dans le bord droit du champ mot de passe (la zone de texte du champ est
    /// rétrécie d'autant). Volontairement pas un Button Unity : un Selectable prendrait la sélection au clic, ce qui
    /// désactive le champ (perte du focus, onEndEdit) ; ici le clic remonte au champ pour la sélection et s'arrête
    /// sur ce composant pour l'action. Fond et police copiés du champ lui-même pour rester dans le style du jeu.
    /// </summary>
    internal sealed class PasswordRevealToggle : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const string ObjectName = "OvoMiam_PasswordReveal";
        private const float Width = 90f;
        private const float Margin = 4f;
        private static readonly Color s_idle = new Color(1f, 1f, 1f, 0.75f);

        private TMP_InputField m_field;
        private TMP_Text m_label;

        /// <summary>Crée le bouton s'il n'existe pas encore sur ce champ, et applique l'état voulu.</summary>
        internal static void Setup(TMP_InputField field)
        {
            Transform existing = field.transform.Find(ObjectName);
            PasswordRevealToggle toggle = existing != null
                ? existing.GetComponent<PasswordRevealToggle>()
                : Build(field);
            toggle.Apply();
        }

        private static PasswordRevealToggle Build(TMP_InputField field)
        {
            GameObject go = new GameObject(ObjectName, typeof(RectTransform), typeof(Image), typeof(PasswordRevealToggle));
            go.transform.SetParent(field.transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-Margin, 0f);
            rect.sizeDelta = new Vector2(Width, -2f * Margin);
            CopyBackground(field.GetComponent<Image>(), go.GetComponent<Image>());
            if (field.textViewport != null)
                field.textViewport.offsetMax += new Vector2(-(Width + Margin), 0f);

            PasswordRevealToggle toggle = go.GetComponent<PasswordRevealToggle>();
            toggle.m_field = field;
            toggle.m_label = BuildLabel(go.transform, field.textComponent);
            return toggle;
        }

        internal static void CopyBackground(Image source, Image target)
        {
            target.raycastTarget = true;
            if (source == null)
            {
                target.color = new Color(0f, 0f, 0f, 0.5f);
                return;
            }
            target.sprite = source.sprite;
            target.type = source.type;
            target.material = source.material;
            target.color = source.color;
        }

        /// <summary>Libellé centré couvrant le parent, police et couleur du champ (atténuées), partagé avec la case Mémoriser.</summary>
        internal static TMP_Text BuildLabel(Transform parent, TMP_Text reference)
        {
            GameObject go = new GameObject("label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            if (reference != null)
            {
                label.font = reference.font;
                label.fontSharedMaterial = reference.fontSharedMaterial;
                label.fontSize = reference.fontSize;
                label.color = reference.color;
            }
            label.alignment = TextAlignmentOptions.Center;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.color *= s_idle;
            return label;
        }

        /// <summary>Met le champ (masqué / en clair) et le libellé en accord avec la config.</summary>
        private void Apply()
        {
            bool show = PasswordRevealConfig.ShowPassword.Value;
            m_field.contentType = show ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
            m_field.ForceLabelUpdate();
            m_label.text = show ? PasswordRevealConfig.HideLabel.Value : PasswordRevealConfig.ShowLabel.Value;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            PasswordRevealConfig.ShowPassword.Value = !PasswordRevealConfig.ShowPassword.Value;
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
