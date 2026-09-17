using UnityEngine;

namespace OvoMiam.Features.MinimapSize
{
    /// <summary>
    /// Décale vers la gauche la liste des indicateurs d'état du HUD (En forme, Repos, Abri… sur la même ligne que
    /// la minicarte, à sa gauche) de la largeur gagnée par la carte agrandie, pour qu'elle ne la recouvre pas.
    /// Les icônes sont positionnées relativement à m_statusEffectListRoot (Hud.UpdateStatusEffects), déplacer la
    /// racine suffit. Hud peut apparaître après Minimap et être recréé au changement de monde : la position
    /// d'origine est mémorisée par instance.
    /// </summary>
    internal static class MinimapSizeHud
    {
        private static Hud s_hud;
        private static Vector2 s_origin;

        /// <summary>À appeler chaque frame : applique le décalage dès que Hud existe et qu'il diffère de l'attendu.</summary>
        public static void Ensure(float scale, float mapWidth)
        {
            Hud hud = Hud.instance;
            if (hud == null || hud.m_statusEffectListRoot == null)
                return;
            RectTransform list = hud.m_statusEffectListRoot;
            if (s_hud != hud)
            {
                s_hud = hud;
                s_origin = list.anchoredPosition;
                Plugin.Log.LogInfo($"Indicateurs d'état : position d'origine {s_origin}, anchors {list.anchorMin}, "
                    + $"minicarte {mapWidth} px de large à l'échelle 1");
            }
            Vector2 expected = s_origin + new Vector2(-(scale - 1f) * mapWidth, 0f);
            if (list.anchoredPosition != expected)
                list.anchoredPosition = expected;
        }
    }
}
