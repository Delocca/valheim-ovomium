using UnityEngine;

namespace Ovomium.Features.MinimapSize
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

        /// <summary>
        /// À appeler chaque frame : applique le décalage dès que Hud existe et qu'il diffère de l'attendu.
        /// <paramref name="appliedScale"/> est l'échelle que la racine de la carte avait au début de la frame : si
        /// elle n'est pas 1 à la première rencontre du Hud (rechargement à chaud sans nettoyage), la liste est déjà
        /// décalée d'autant et l'origine s'en déduit.
        /// </summary>
        public static void Ensure(float scale, float mapWidth, float appliedScale)
        {
            Hud hud = Hud.instance;
            if (hud == null || hud.m_statusEffectListRoot == null)
                return;
            RectTransform list = hud.m_statusEffectListRoot;
            if (s_hud != hud)
            {
                s_hud = hud;
                s_origin = list.anchoredPosition - Shift(appliedScale, mapWidth);
                Plugin.Log.LogInfo($"Indicateurs d'état : position d'origine {s_origin}, anchors {list.anchorMin}, "
                    + $"minicarte {mapWidth} px de large à l'échelle 1");
            }
            Vector2 expected = s_origin + Shift(scale, mapWidth);
            if (list.anchoredPosition != expected)
                list.anchoredPosition = expected;
        }

        /// <summary>Remet la liste à sa position d'origine et oublie l'instance (désactivation, déchargement).</summary>
        public static void Restore()
        {
            if (s_hud != null && s_hud.m_statusEffectListRoot != null)
                s_hud.m_statusEffectListRoot.anchoredPosition = s_origin;
            s_hud = null;
        }

        private static Vector2 Shift(float scale, float mapWidth)
        {
            return new Vector2(-(scale - 1f) * mapWidth, 0f);
        }
    }
}
