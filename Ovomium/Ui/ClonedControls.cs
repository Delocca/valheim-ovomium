using UnityEngine;
using UnityEngine.UI;

namespace Ovomium.Ui
{
    /// <summary>Événements neufs pour un contrôle vanilla cloné ou détourné, son de clic compris.</summary>
    internal static class ClonedControls
    {
        /// <summary>Remplace onClick (les listeners persistants du modèle visent le composant vanilla).</summary>
        public static Button.ButtonClickedEvent NewOnClick(Button button)
        {
            button.onClick = new Button.ButtonClickedEvent();
            RebindClickSound(button);
            return button.onClick;
        }

        public static Toggle.ToggleEvent NewOnValueChanged(Toggle toggle)
        {
            toggle.onValueChanged = new Toggle.ToggleEvent();
            RebindClickSound(toggle);
            return toggle.onValueChanged;
        }

        /// <summary>
        /// ButtonSfx (depuis Valheim 1.0.17) s'abonne à onClick / onValueChanged dans OnEnable : un clone actif
        /// est abonné à l'événement remplacé, et RemoveAllListeners le désabonne. Désactiver puis réactiver
        /// le réabonne à l'événement courant (sans effet sur un objet inactif, qui s'abonnera à son activation).
        /// </summary>
        public static void RebindClickSound(Component control)
        {
            var sfx = control.GetComponent<ButtonSfx>();
            if (sfx == null || !sfx.isActiveAndEnabled) return;
            sfx.enabled = false;
            sfx.enabled = true;
        }
    }
}
