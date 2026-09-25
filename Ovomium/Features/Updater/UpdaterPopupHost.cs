using TMPro;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Fenêtre <c>UnifiedPopup</c> du mod en cours (une seule à la fois) : agrandie (<see cref="UpdaterPopupLayout"/>),
    /// corps aligné à gauche (centré par <c>ResetUI</c> à chaque affichage : rien à restaurer), retirée de la pile
    /// seulement si elle est au sommet, pour ne jamais fermer une fenêtre vanilla. Retirée au déchargement : la version
    /// suivante la réaffiche d'après <see cref="UpdateState"/>.
    /// </summary>
    internal static class UpdaterPopupHost
    {
        private static PopupBase s_popup;

        /// <summary>Notre fenêtre est affichée, au sommet de la pile.</summary>
        public static bool IsShown(PopupBase popup = null)
        {
            UnifiedPopup instance = UnifiedPopup.instance;
            return s_popup != null && (popup == null || popup == s_popup) && instance != null
                && instance.popupStack.Count > 0 && instance.popupStack.Peek() == s_popup && UnifiedPopup.IsVisible();
        }

        public static void Show(PopupBase popup)
        {
            Close();
            UnifiedPopup.Push(popup);
            s_popup = popup;
            UpdaterPopupLayout.Apply();
            SetBody(null);
        }

        /// <summary>Remplace le texte du corps de notre fenêtre (progression) ; null : réaligne seulement.</summary>
        public static void SetBody(string text)
        {
            TextMeshProUGUI body = UnifiedPopup.instance != null ? UnifiedPopup.instance.bodyText : null;
            if (body == null)
                return;
            body.alignment = TextAlignmentOptions.TopLeft;
            if (text != null && body.text != text)
                body.text = text;
        }

        public static void Close()
        {
            UpdaterPopupLayout.Restore();
            if (IsShown())
                UnifiedPopup.Pop();
            s_popup = null;
        }
    }
}
