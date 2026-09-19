using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.MinimapSize
{
    /// <summary>
    /// Shift + touches de zoom de la carte (actions MapZoomIn / MapZoomOut, pavé num + / - par défaut) change
    /// l'échelle (localScale) de la racine de la petite carte, avec répétition si la touche est maintenue
    /// (KeyRepeat). La valeur persistée est réappliquée dès que la racine existe (chargement, changement de monde).
    /// La densité terrain / pixel est conservée : la petite carte affiche uvRect = SmallZoom (Minimap.CenterMap),
    /// donc SmallZoom suit l'échelle, et m_minZoom est abaissé / relevé d'autant en mode Small pour rester
    /// atteignable. Ces actions déclenchent aussi le zoom vanilla (UpdateMap) sur la frame de l'appui : le Prefix
    /// mémorise SmallZoom, le Postfix le restaure sur cette frame seulement (pas lors des répétitions).
    /// Les icônes de la carte gardent leur taille vanilla (MinimapSizeIcons) et les indicateurs d'état du HUD sont
    /// poussés à gauche de la carte agrandie (MinimapSizeHud).
    /// Tout est restauré par <see cref="Restore"/> (option désactivée en jeu, déchargement à chaud).
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update), new System.Type[0])]
    internal static class MinimapSizePatch
    {
        /// <summary>Valeurs du jeu avant modification, à mémoriser par instance de Minimap (recréée au changement de monde).</summary>
        private struct Vanilla
        {
            public float MinZoom;
            public float PinSize;
            /// <summary>Faux si la racine était déjà modifiée à la première rencontre : pivot d'origine inconnu.</summary>
            public bool PivotKnown;
            public Vector2 Pivot;
            public Vector2 AnchoredPosition;
        }

        /// <summary>Valeur du prefab (initialiseur du champ), seule source si la racine était déjà modifiée à la rencontre.</summary>
        private const float PrefabMinZoom = 0.01f;

        private static readonly KeyRepeat s_zoomIn = new KeyRepeat("MapZoomIn");
        private static readonly KeyRepeat s_zoomOut = new KeyRepeat("MapZoomOut");
        private static float s_zoomBeforeUpdate;
        private static Minimap s_vanillaInstance;
        private static Vanilla s_vanilla;

        private static void Prefix(Minimap __instance)
        {
            s_zoomBeforeUpdate = __instance.SmallZoom;
        }

        private static void Postfix(Minimap __instance)
        {
            if (!MinimapSizeConfig.Enabled.Value)
            {
                if (s_vanillaInstance == __instance)
                    Restore();
                return;
            }
            RectTransform root = SmallRoot(__instance);
            if (root == null)
                return;
            Vanilla vanilla = RememberVanilla(__instance, root);
            float appliedScale = root.localScale.x;

            bool resizing = __instance.m_mode == Minimap.MapMode.Small && ShiftHeld() && CanResize();
            int direction = (s_zoomIn.Step(resizing) ? 1 : 0) - (s_zoomOut.Step(resizing) ? 1 : 0);
            if (resizing && (s_zoomIn.PressedThisFrame || s_zoomOut.PressedThisFrame))
                __instance.SmallZoom = s_zoomBeforeUpdate;
            if (direction != 0)
                StepScale(direction);

            float scale = MinimapSizeConfig.Scale.Value;
            if (!Mathf.Approximately(appliedScale, scale))
                Apply(__instance, root, scale, vanilla.PinSize);
            UpdateMinZoom(__instance, scale, vanilla.MinZoom);
            MinimapSizeHud.Ensure(scale, root.rect.width, appliedScale);
        }

        /// <summary>
        /// Remet la minicarte et le HUD dans l'état vanilla mémorisé (échelle, zoom, pivot, icônes, indicateurs
        /// d'état) et oublie l'instance. Sans effet si rien n'a été touché ou si Minimap n'existe plus.
        /// Appelée à la désactivation de l'option à chaud et par <see cref="Unload"/>.
        /// </summary>
        public static void Restore()
        {
            Minimap map = Minimap.instance;
            if (map != null && s_vanillaInstance == map)
                RestoreMap(map, SmallRoot(map));
            s_vanillaInstance = null;
            MinimapSizeHud.Restore();
        }

        /// <summary>Déchargement du plugin (rechargement à chaud) : rend la scène vanilla.</summary>
        internal static void Unload() => Restore();

        private static void RestoreMap(Minimap map, RectTransform root)
        {
            map.m_minZoom = s_vanilla.MinZoom;
            MinimapSizeIcons.Apply(map, 1f, s_vanilla.PinSize);
            if (root == null)
                return;
            float scale = root.localScale.x;
            root.localScale = Vector3.one;
            if (scale > 0f)
                map.SmallZoom /= scale;
            if (s_vanilla.PivotKnown)
            {
                root.pivot = s_vanilla.Pivot;
                root.anchoredPosition = s_vanilla.AnchoredPosition;
            }
        }

        private static RectTransform SmallRoot(Minimap map)
        {
            return map.m_smallRoot == null ? null : map.m_smallRoot.GetComponent<RectTransform>();
        }

        private static void StepScale(int direction)
        {
            float scale = MinimapSizeConfig.Scale.Value + direction * MinimapSizeConfig.Step.Value;
            scale = Mathf.Clamp(scale, MinimapSizeConfig.MinScale, MinimapSizeConfig.MaxScale);
            MinimapSizeConfig.Scale.Value = Mathf.Round(scale * 100f) / 100f;
        }

        private static bool ShiftHeld()
        {
            return ZInput.GetKey(KeyCode.LeftShift) || ZInput.GetKey(KeyCode.RightShift);
        }

        /// <summary>Mêmes gardes que Minimap.Update pour ses propres raccourcis (chat, console, menu, inventaire…).</summary>
        private static bool CanResize()
        {
            return (Chat.instance == null || !Chat.instance.HasFocus())
                && !Console.IsVisible()
                && !TextInput.IsVisible()
                && !Menu.IsActive()
                && !InventoryGui.IsVisible()
                && !Minimap.InTextInput();
        }

        /// <summary>
        /// Change l'échelle du cadre, ajuste SmallZoom du même ratio (même terrain par pixel) et compense les icônes.
        /// Appelé aussi à la création de la racine (chargement, changement de monde) si l'échelle persistée n'est pas 1.
        /// </summary>
        private static void Apply(Minimap map, RectTransform root, float scale, float vanillaPinSize)
        {
            AnchorPivotToCorner(root);
            float ratio = scale / root.localScale.x;
            root.localScale = new Vector3(scale, scale, 1f);
            map.m_minZoom = MinimapSizeConfig.MinZoom.Value * scale;
            map.SmallZoom *= ratio;
            MinimapSizeIcons.Apply(map, scale, vanillaPinSize);
        }

        /// <summary>
        /// m_minZoom borne aussi LargeZoom : la borne configurée (MinZoom, mise à l'échelle) ne s'applique qu'en
        /// mode Small, pour laisser la grande carte vanilla.
        /// </summary>
        private static void UpdateMinZoom(Minimap map, float scale, float vanillaMinZoom)
        {
            map.m_minZoom = map.m_mode == Minimap.MapMode.Small ? MinimapSizeConfig.MinZoom.Value * scale : vanillaMinZoom;
        }

        /// <summary>
        /// Mémorise les valeurs vanilla à la première rencontre de l'instance. Si la racine est déjà mise à
        /// l'échelle (rechargement à chaud sans nettoyage par l'ancienne version), les valeurs lues sont déjà
        /// modifiées : taille des pins retrouvée par calcul inverse, m_minZoom pris du prefab, pivot inconnu.
        /// </summary>
        private static Vanilla RememberVanilla(Minimap map, RectTransform root)
        {
            if (s_vanillaInstance == map)
                return s_vanilla;
            s_vanillaInstance = map;
            float applied = root.localScale.x;
            bool untouched = Mathf.Approximately(applied, 1f);
            s_vanilla = new Vanilla
            {
                MinZoom = untouched ? map.m_minZoom : PrefabMinZoom,
                PinSize = untouched ? map.m_pinSizeSmall : map.m_pinSizeSmall * applied,
                PivotKnown = untouched,
                Pivot = root.pivot,
                AnchoredPosition = root.anchoredPosition,
            };
            if (!untouched)
                Plugin.Log.LogWarning($"Minicarte : racine déjà à l'échelle {applied} à la rencontre, valeurs vanilla reconstruites");
            return s_vanilla;
        }

        /// <summary>
        /// Place le pivot sur le coin d'ancrage (ancrage ponctuel uniquement) pour que la carte grandisse vers
        /// l'intérieur de l'écran, sans déplacer sa position visuelle. Sans effet si le pivot y est déjà.
        /// </summary>
        private static void AnchorPivotToCorner(RectTransform root)
        {
            if (root.anchorMin != root.anchorMax)
                return;
            Vector2 pivot = new Vector2(Mathf.Round(root.anchorMin.x), Mathf.Round(root.anchorMin.y));
            if (root.pivot == pivot)
                return;
            Plugin.Log.LogInfo($"Minicarte : anchors {root.anchorMin}, pivot {root.pivot} → {pivot}, "
                + $"taille {root.rect.size}, position {root.anchoredPosition}");
            Vector2 shift = (pivot - root.pivot) * root.rect.size * root.localScale.x;
            root.pivot = pivot;
            root.anchoredPosition += shift;
        }
    }
}
