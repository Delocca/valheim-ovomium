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
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update), new System.Type[0])]
    internal static class MinimapSizePatch
    {
        /// <summary>Valeurs du jeu avant modification, à mémoriser par instance de Minimap (recréée au changement de monde).</summary>
        private struct Vanilla
        {
            public float MinZoom;
            public float PinSize;
        }

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
            if (!MinimapSizeConfig.Enabled.Value || __instance.m_smallRoot == null)
                return;
            RectTransform root = __instance.m_smallRoot.GetComponent<RectTransform>();
            if (root == null)
                return;

            bool resizing = __instance.m_mode == Minimap.MapMode.Small && ShiftHeld() && CanResize();
            int direction = (s_zoomIn.Step(resizing) ? 1 : 0) - (s_zoomOut.Step(resizing) ? 1 : 0);
            if (resizing && (s_zoomIn.PressedThisFrame || s_zoomOut.PressedThisFrame))
                __instance.SmallZoom = s_zoomBeforeUpdate;
            if (direction != 0)
                StepScale(direction);

            float scale = MinimapSizeConfig.Scale.Value;
            if (!Mathf.Approximately(root.localScale.x, scale))
                Apply(__instance, root, scale);
            UpdateMinZoom(__instance, scale);
            MinimapSizeHud.Ensure(scale, root.rect.width);
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
        private static void Apply(Minimap map, RectTransform root, float scale)
        {
            AnchorPivotToCorner(root);
            float ratio = scale / root.localScale.x;
            root.localScale = new Vector3(scale, scale, 1f);
            Vanilla vanilla = RememberVanilla(map);
            map.m_minZoom = MinimapSizeConfig.MinZoom.Value * scale;
            map.SmallZoom *= ratio;
            MinimapSizeIcons.Apply(map, scale, vanilla.PinSize);
        }

        /// <summary>
        /// m_minZoom borne aussi LargeZoom : la borne configurée (MinZoom, mise à l'échelle) ne s'applique qu'en
        /// mode Small, pour laisser la grande carte vanilla.
        /// </summary>
        private static void UpdateMinZoom(Minimap map, float scale)
        {
            float vanillaMinZoom = RememberVanilla(map).MinZoom;
            map.m_minZoom = map.m_mode == Minimap.MapMode.Small ? MinimapSizeConfig.MinZoom.Value * scale : vanillaMinZoom;
        }

        private static Vanilla RememberVanilla(Minimap map)
        {
            if (s_vanillaInstance != map)
            {
                s_vanillaInstance = map;
                s_vanilla = new Vanilla { MinZoom = map.m_minZoom, PinSize = map.m_pinSizeSmall };
            }
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
