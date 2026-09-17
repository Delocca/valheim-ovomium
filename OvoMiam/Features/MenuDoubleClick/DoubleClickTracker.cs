using System.Collections.Generic;
using UnityEngine;

namespace OvoMiam.Features.MenuDoubleClick
{
    /// <summary>
    /// Détecte un double-clic sur une même entrée : deux clics sur la même clé à moins de
    /// <see cref="MenuDoubleClickConfig.DoubleClickSeconds"/> d'écart. Le clic qui complète un double-clic
    /// remet le compteur à zéro (un troisième clic ne compte pas comme un nouveau double-clic).
    /// </summary>
    internal sealed class DoubleClickTracker<T>
    {
        private bool m_hasLast;
        private T m_lastKey;
        private float m_lastTime;

        /// <summary>Enregistre un clic ; vrai si c'est le second d'un double-clic sur <paramref name="key"/>.</summary>
        public bool Click(T key)
        {
            float now = Time.unscaledTime;
            bool isDouble = m_hasLast && EqualityComparer<T>.Default.Equals(m_lastKey, key)
                && now - m_lastTime <= MenuDoubleClickConfig.DoubleClickSeconds.Value;
            m_hasLast = !isDouble;
            m_lastKey = key;
            m_lastTime = now;
            return isDouble;
        }
    }
}
