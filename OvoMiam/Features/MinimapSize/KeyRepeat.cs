using UnityEngine;

namespace OvoMiam.Features.MinimapSize
{
    /// <summary>
    /// Répétition d'une action ZInput maintenue : premier pas sur GetButtonDown (réactivité), puis, tant que la
    /// touche reste enfoncée et que la condition est vraie, un pas après un délai initial puis à intervalle fixe.
    /// Une touche déjà enfoncée quand la condition devient vraie ne répète pas (il faut un nouvel appui).
    /// </summary>
    internal sealed class KeyRepeat
    {
        private const float InitialDelay = 0.4f;
        private const float Interval = 0.08f;

        private readonly string m_button;
        private float m_nextRepeat = float.PositiveInfinity;

        public KeyRepeat(string button)
        {
            m_button = button;
        }

        /// <summary>Vrai sur la frame de l'appui initial (GetButtonDown).</summary>
        public bool PressedThisFrame => ZInput.GetButtonDown(m_button);

        /// <summary>Vrai si un pas doit être exécuté cette frame. <paramref name="allowed"/> faux réarme la répétition.</summary>
        public bool Step(bool allowed)
        {
            if (!allowed || !ZInput.GetButton(m_button))
            {
                m_nextRepeat = float.PositiveInfinity;
                return false;
            }
            float now = Time.unscaledTime;
            if (ZInput.GetButtonDown(m_button))
            {
                m_nextRepeat = now + InitialDelay;
                return true;
            }
            if (now < m_nextRepeat)
                return false;
            m_nextRepeat = now + Interval;
            return true;
        }
    }
}
