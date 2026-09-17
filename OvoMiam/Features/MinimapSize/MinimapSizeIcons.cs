using UnityEngine;

namespace OvoMiam.Features.MinimapSize
{
    /// <summary>
    /// Garde les icônes de la petite carte à leur taille vanilla malgré le localScale de m_smallRoot, qui agrandit
    /// tout : pins (m_pinSizeSmall, lu par UpdatePins à la création du marqueur ; les marqueurs existants sont
    /// redimensionnés en place, comme le fait UpdatePins), marqueur du joueur et du bateau (localScale inverse ;
    /// le jeu ne touche que leur rotation). Le nom du biome (m_biomeNameSmall) reste à l'échelle.
    /// </summary>
    internal static class MinimapSizeIcons
    {
        public static void Apply(Minimap map, float scale, float vanillaPinSize)
        {
            map.m_pinSizeSmall = vanillaPinSize / scale;
            foreach (Minimap.PinData pin in map.m_pins)
            {
                if (pin.m_uiElement == null || pin.m_uiElement.parent != map.m_pinRootSmall || pin.m_worldSize > 0f)
                    continue;
                float size = pin.m_doubleSize ? map.m_pinSizeSmall * 2f : map.m_pinSizeSmall;
                pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            }
            Vector3 inverse = new Vector3(1f / scale, 1f / scale, 1f);
            if (map.m_smallMarker != null)
                map.m_smallMarker.localScale = inverse;
            if (map.m_smallShipMarker != null)
                map.m_smallShipMarker.localScale = inverse;
        }
    }
}
