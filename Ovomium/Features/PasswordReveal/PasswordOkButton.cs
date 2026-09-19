using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ovomium.Features.PasswordReveal
{
    /// <summary>
    /// Bouton « OK » sous le champ mot de passe, aligné à droite (la case Mémoriser occupe la gauche de la même
    /// ligne). Enfant du champ et pas un Button Unity, pour les mêmes raisons que le bouton Afficher / Masquer.
    /// Le clic fait exactement ce que fait Entrée : ZNet.OnPasswordEntered(texte du champ) — le chemin qu'emprunte
    /// le listener OnInputSubmit vanilla, donc le Postfix de <see cref="PasswordRevealPatch"/> (mémorisation)
    /// s'applique aussi, et un mot de passe vide ne ferme rien.
    /// </summary>
    internal sealed class PasswordOkButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const string ObjectName = "Ovomium_PasswordOk";
        private const float Width = 90f;
        private const float Height = 26f;
        /// <summary>Espace entre le bas du champ et le haut du bouton (même valeur que la case Mémoriser).</summary>
        private const float Gap = 6f;
        private static readonly Color s_idle = new Color(1f, 1f, 1f, 0.75f);

        private TMP_InputField m_field;
        private TMP_Text m_label;

        /// <summary>Pose le bouton sur ce champ (un bouton déjà présent est retiré puis reconstruit) et pose son libellé.</summary>
        internal static void Setup(TMP_InputField field)
        {
            Remove(field);
            Build(field).m_label.text = PasswordRevealConfig.OkLabel.Value;
        }

        /// <summary>Retire le bouton s'il est présent.</summary>
        internal static void Remove(TMP_InputField field)
        {
            Transform existing = field.transform.Find(ObjectName);
            if (existing != null)
                Object.Destroy(existing.gameObject);
        }

        private static PasswordOkButton Build(TMP_InputField field)
        {
            GameObject go = new GameObject(ObjectName, typeof(RectTransform), typeof(Image), typeof(PasswordOkButton));
            go.transform.SetParent(field.transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(0f, -Gap);
            rect.sizeDelta = new Vector2(Width, Height);
            PasswordRevealToggle.CopyBackground(field.GetComponent<Image>(), go.GetComponent<Image>());

            PasswordOkButton button = go.GetComponent<PasswordOkButton>();
            button.m_field = field;
            button.m_label = PasswordRevealToggle.BuildLabel(go.transform, field.textComponent);
            return button;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            ZNet znet = ZNet.instance;
            // m_tempPasswordRPC est posé à l'ouverture du dialogue et vidé à sa fermeture : sans lui, rien à soumettre.
            if (znet == null || znet.m_tempPasswordRPC == null)
                return;
            znet.OnPasswordEntered(m_field.text);
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
