using UnityEngine;
using UnityEngine.EventSystems;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>
    /// Posé sur un curseur de la fenêtre Ovomium : pendant le glissement (bouton enfoncé), la fenêtre Paramètres et
    /// le menu de jeu derrière elle deviennent presque transparents pour voir l'effet du réglage sur le rendu.
    /// Les handlers d'un même GameObject sont tous exécutés : le Slider garde son propre glissement.
    /// </summary>
    internal sealed class SliderPeek : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private const float PeekAlpha = 0.1f;

        public void OnPointerDown(PointerEventData eventData) => SetAlpha(PeekAlpha);
        public void OnPointerUp(PointerEventData eventData) => SetAlpha(1f);
        private void OnDisable() => SetAlpha(1f);

        private void SetAlpha(float alpha)
        {
            var settings = GetComponentInParent<Settings>();
            if (settings != null)
                GroupOf(settings.gameObject).alpha = alpha;
            if (Menu.instance != null && Menu.instance.m_root != null)
                GroupOf(Menu.instance.m_root.gameObject).alpha = alpha;
        }

        private static CanvasGroup GroupOf(GameObject go)
            => go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
    }
}
