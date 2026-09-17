using HarmonyLib;
using UnityEngine;

namespace OvoMiam.Features.MapZoomToCursor
{
    /// <summary>
    /// Zoom de la grande carte autour du point du monde sous le curseur (molette et actions MapZoomIn / MapZoomOut).
    /// Minimap.UpdateMap applique le zoom (LargeZoom) puis appelle CenterMap(player + m_mapOffset), qui pose le
    /// uvRect ; ScreenToWorldPoint lit ce uvRect, donc en Prefix il décrit encore la vue d'avant le zoom. Le Prefix
    /// mémorise le zoom et le décalage curseur → centre de vue (d) ; le Postfix, si le zoom a changé d'un facteur f,
    /// déplace m_mapOffset de d · (1 − f) (le point sous le curseur reste à la même place à l'écran) puis rappelle
    /// CenterMap pour que l'affichage de la frame soit déjà juste. Le jeu contient le même calcul sous
    /// <c>if (false &amp;&amp; …)</c> : la fonctionnalité existe, désactivée. Manette et tactile sont laissés vanilla
    /// (pas de curseur ; le tactile a son propre zoom vers le pincement).
    /// </summary>
    [HarmonyPatch(typeof(Minimap), "UpdateMap", typeof(Player), typeof(float), typeof(bool))]
    internal static class MapZoomToCursorPatch
    {
        private static float s_zoomBefore;
        private static Vector3 s_cursorFromCenter;
        private static bool s_cursorOnMap;

        private static void Prefix(Minimap __instance)
        {
            s_cursorOnMap = false;
            if (!MapZoomToCursorConfig.Enabled.Value || !IsMouseOnLargeMap(__instance))
                return;
            s_zoomBefore = __instance.LargeZoom;
            Vector2 pointer = ZInput.pointerPosition;
            s_cursorFromCenter = __instance.ScreenToWorldPoint(pointer) - __instance.GetViewCenterWorldPoint();
            s_cursorOnMap = true;
        }

        private static void Postfix(Minimap __instance, Player player)
        {
            if (!s_cursorOnMap || __instance.m_mode != Minimap.MapMode.Large)
                return;
            float zoom = __instance.LargeZoom;
            if (Mathf.Approximately(zoom, s_zoomBefore) || s_zoomBefore <= 0f)
                return;
            float factor = zoom / s_zoomBefore;
            __instance.m_mapOffset += s_cursorFromCenter * (1f - factor);
            __instance.CenterMap(player.transform.position + __instance.m_mapOffset);
            if (__instance.m_dragView)
                __instance.m_dragWorldPos = __instance.ScreenToWorldPoint(ZInput.pointerPosition);
        }

        /// <summary>Grande carte ouverte, souris (ni manette ni tactile) et curseur dans l'image de la carte.</summary>
        private static bool IsMouseOnLargeMap(Minimap map)
        {
            if (map.m_mode != Minimap.MapMode.Large || map.m_mapImageLarge == null
                || ZInput.IsGamepadActive() || ZInput.IsTouchActive())
                return false;
            RectTransform rect = map.m_mapImageLarge.transform as RectTransform;
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, ZInput.pointerPosition, null);
        }
    }
}
