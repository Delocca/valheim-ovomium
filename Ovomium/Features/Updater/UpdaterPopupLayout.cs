using System.Text;
using TMPro;
using UnityEngine;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Agrandit la fenêtre <c>UnifiedPopup</c> le temps d'afficher le changelog. Relevé en jeu : corps
    /// <c>BodyText</c> étiré en largeur, hauteur fixe 140, centré dans <c>Popup</c> (400×300, centré, non étiré) sous
    /// <c>PopupBlockingBackground</c> plein écran. Police du corps fixée (le prefab la rétrécit pour faire tenir le
    /// texte), largeur du panneau × <c>PopupScale</c>, hauteur du corps = hauteur préférée du texte mesurée à cette
    /// largeur, panneau rehaussé d'autant (chrome en-tête + boutons constant), plafonné à 90 % du canvas (le corps
    /// passe alors en ellipse). L'instance est un singleton réutilisé par les fenêtres vanilla : tout est restauré
    /// avant le <c>Pop()</c>.
    /// </summary>
    internal static class UpdaterPopupLayout
    {
        private const float BodyMargin = 8f;
        private const float MaxCanvasShare = 0.9f;

        private static TextMeshProUGUI s_body;
        private static RectTransform s_panel;
        private static bool s_autoSizing;
        private static float s_fontSize;
        private static TextOverflowModes s_overflow;
        private static Vector2 s_bodySize, s_panelSize;
        private static bool s_logged;

        /// <summary>À appeler juste après le <c>Push</c> de notre fenêtre (texte posé).</summary>
        public static void Apply()
        {
            UnifiedPopup popup = UnifiedPopup.instance;
            if (popup == null || popup.bodyText == null)
                return;
            if (s_panel != null || s_body != null)
                Restore();  // fenêtre précédente emportée sans réponse
            if (!s_logged)
            {
                s_logged = true;
                LogHierarchy(popup);
            }
            RectTransform panel = FindPanel(popup);
            if (panel == null)
                return;
            s_body = popup.bodyText;
            s_panel = panel;
            s_autoSizing = s_body.enableAutoSizing;
            s_fontSize = s_body.fontSize;
            s_overflow = s_body.overflowMode;
            s_bodySize = s_body.rectTransform.sizeDelta;
            s_panelSize = panel.sizeDelta;
            Resize(s_body, panel);
        }

        /// <summary>Remet tout ce qui a été touché ; sans effet si rien ne l'a été (objets détruits ignorés).</summary>
        public static void Restore()
        {
            if (s_body != null)
            {
                s_body.enableAutoSizing = s_autoSizing;
                s_body.fontSize = s_fontSize;
                s_body.overflowMode = s_overflow;
                s_body.rectTransform.sizeDelta = s_bodySize;
            }
            if (s_panel != null)
                s_panel.sizeDelta = s_panelSize;
            s_body = null;
            s_panel = null;
        }

        /// <summary>Premier ancêtre du corps non étiré sur les deux axes (« Popup »), au plus <c>popupUIParent</c>.</summary>
        private static RectTransform FindPanel(UnifiedPopup popup)
        {
            for (Transform t = popup.bodyText.transform.parent; t != null; t = t.parent)
            {
                if (t is RectTransform rect && !Stretched(rect, 0) && !Stretched(rect, 1))
                    return rect;
                if (popup.popupUIParent != null && t == popup.popupUIParent.transform)
                    break;
            }
            Plugin.Log.LogWarning("Updater : panneau de la fenêtre introuvable, taille vanilla conservée");
            return null;
        }

        private static bool Stretched(RectTransform rect, int axis)
        {
            return !Mathf.Approximately(rect.anchorMin[axis], rect.anchorMax[axis]);
        }

        private static void Resize(TextMeshProUGUI body, RectTransform panel)
        {
            body.enableAutoSizing = false;
            body.fontSize = UpdaterConfig.PopupFontSize.Value;
            // Largeur d'abord : la hauteur préférée dépend du retour à la ligne à la largeur finale.
            panel.sizeDelta = new Vector2(s_panelSize.x * UpdaterConfig.PopupScale.Value, s_panelSize.y);
            body.ForceMeshUpdate();
            float chrome = s_panelSize.y - body.rectTransform.rect.height;
            float wanted = body.preferredHeight + BodyMargin;
            float maxPanel = CanvasHeight(panel) * MaxCanvasShare;
            float bodyHeight = Mathf.Min(wanted, maxPanel - chrome);
            if (bodyHeight < wanted)
                body.overflowMode = TextOverflowModes.Ellipsis;
            body.rectTransform.sizeDelta = new Vector2(s_bodySize.x, bodyHeight);
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, chrome + bodyHeight);
        }

        private static float CanvasHeight(RectTransform panel)
        {
            Canvas canvas = panel.GetComponentInParent<Canvas>();
            var root = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            return root != null ? root.rect.height : Screen.height;
        }

        /// <summary>Relevé du prefab pour le réglage fin : une ligne par niveau, avant modification.</summary>
        private static void LogHierarchy(UnifiedPopup popup)
        {
            TextMeshProUGUI body = popup.bodyText;
            Plugin.Log.LogInfo($"Updater : bodyText autoSize={body.enableAutoSizing} fontSize={body.fontSize} "
                + $"min/max={body.fontSizeMin}/{body.fontSizeMax} size={body.rectTransform.rect.size} "
                + $"overflow={body.overflowMode}");
            int depth = 0;
            for (Transform t = body.transform; t != null; t = t.parent)
            {
                if (t is RectTransform rect)
                    Plugin.Log.LogInfo($"Updater : popup [{depth++}] {Describe(rect)}");
                if (popup.popupUIParent != null && t == popup.popupUIParent.transform)
                    break;
            }
        }

        private static string Describe(RectTransform rect)
        {
            var sb = new StringBuilder(rect.name).Append(" comps={");
            foreach (Component component in rect.GetComponents<Component>())
                if (!(component is Transform) && !(component is CanvasRenderer))
                    sb.Append(component.GetType().Name).Append(", ");
            sb.Append("} anchors=").Append(rect.anchorMin).Append('-').Append(rect.anchorMax)
              .Append(" pivot=").Append(rect.pivot).Append(" pos=").Append(rect.anchoredPosition)
              .Append(" sizeDelta=").Append(rect.sizeDelta).Append(" size=").Append(rect.rect.size);
            return sb.ToString();
        }
    }
}
