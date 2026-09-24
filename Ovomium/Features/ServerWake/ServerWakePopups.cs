using System;

namespace Ovomium.Features.ServerWake
{
    /// <summary>
    /// Fenêtres vanilla (<c>UnifiedPopup</c>) du réveil : attente annulable au texte vivant, choix après un échec.
    /// Les callbacks vanilla ne ferment pas la fenêtre : <see cref="Close"/> la retire si elle est au sommet de la pile.
    /// </summary>
    internal static class ServerWakePopups
    {
        /// <summary>Attente « Réveil de … » avec bouton Annuler ; textes relus à chaque frame.</summary>
        public static PopupBase ShowWaiting(Func<string> header, Func<string> body, Action cancel)
        {
            if (!UnifiedPopup.IsAvailable())
                return null;
            var popup = new CancelableTaskPopup(() => header(), () => body(), () => false, () => cancel());
            UnifiedPopup.Push(popup);
            return popup;
        }

        /// <summary>Deux choix aux libellés libres (bouton droit = <paramref name="rightLabel"/>).</summary>
        public static PopupBase ShowChoice(string header, string text, string rightLabel, Action right, string leftLabel, Action left)
        {
            if (!UnifiedPopup.IsAvailable())
                return null;
            var popup = new YesNoPopup(header, text, () => right(), () => left(), localizeText: false);
            UnifiedPopup.Push(popup);
            UnifiedPopup.instance.buttonRightText.text = rightLabel;
            UnifiedPopup.instance.buttonLeftText.text = leftLabel;
            return popup;
        }

        /// <summary>Retire <paramref name="popup"/> s'il est la fenêtre affichée.</summary>
        public static void Close(PopupBase popup)
        {
            if (popup == null || !UnifiedPopup.IsAvailable())
                return;
            var stack = UnifiedPopup.instance.popupStack;
            if (stack.Count > 0 && stack.Peek() == popup)
                UnifiedPopup.Pop();
        }
    }
}
