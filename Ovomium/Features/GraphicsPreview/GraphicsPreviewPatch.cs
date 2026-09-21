using HarmonyLib;
using Ovomium.Features.SettingsMenu;
using Valheim.SettingsGui;

namespace Ovomium.Features.GraphicsPreview
{
    /// <summary>
    /// Ouverture de l'onglet : mémorise les réglages en vigueur, et pose <see cref="SliderPeek"/> sur les curseurs
    /// de qualité (pas celui de la limite d'images par seconde, sans effet visible) : fenêtre effacée pendant le glissement.
    /// </summary>
    [HarmonyPatch(typeof(GraphicsSettings), nameof(GraphicsSettings.Initialize), new System.Type[0])]
    internal static class GraphicsPreviewInitializePatch
    {
        private static void Postfix(GraphicsSettings __instance)
        {
            GraphicsPreview.Begin();
            foreach (var slider in __instance.m_dynamicQualitySliders)
                if (slider != null && slider.GetComponent<SliderPeek>() == null)
                    slider.gameObject.AddComponent<SliderPeek>();
        }
    }

    /// <summary>Un réglage entier (liste, curseur) modifié dans l'onglet.</summary>
    [HarmonyPatch(typeof(GraphicsSettings), "ModifySetting", new[] { typeof(GraphicsSettingInt), typeof(int) })]
    internal static class GraphicsPreviewModifyIntPatch
    {
        private static void Postfix() => GraphicsPreview.MarkDirty();
    }

    /// <summary>Une bascule modifiée dans l'onglet.</summary>
    [HarmonyPatch(typeof(GraphicsSettings), "ModifySetting", new[] { typeof(GraphicsSettingBool), typeof(bool) })]
    internal static class GraphicsPreviewModifyBoolPatch
    {
        private static void Postfix() => GraphicsPreview.MarkDirty();
    }

    /// <summary>
    /// Changement de préréglage par les flèches : ne touche pas <c>m_currentSettingsRaw</c> (l'UI est posée sans
    /// notification), le préréglage est appliqué par-dessus le brut au moment de l'application, comme pour OK.
    /// </summary>
    [HarmonyPatch(typeof(GraphicsSettings), nameof(GraphicsSettings.ChangePreset), new[] { typeof(int) })]
    internal static class GraphicsPreviewChangePresetPatch
    {
        private static void Postfix() => GraphicsPreview.MarkDirty();
    }

    /// <summary>Une application au plus par frame, quel que soit le nombre de changements.</summary>
    [HarmonyPatch(typeof(GraphicsSettings), "Update", new System.Type[0])]
    internal static class GraphicsPreviewUpdatePatch
    {
        private static void Postfix(GraphicsSettings __instance) => GraphicsPreview.Flush(__instance);
    }

    /// <summary>
    /// <c>UpdateUI</c> est abonnée à <c>GraphicsSettingsChanged</c> : elle écraserait l'état en cours d'édition avec
    /// celui du manager et rallongerait <c>m_qualityDropdowns</c> à chaque appel (bug vanilla de
    /// <c>PopulateRenderScales</c>). Sautée pendant notre application.
    /// </summary>
    [HarmonyPatch(typeof(GraphicsSettings), "UpdateUI", new System.Type[0])]
    internal static class GraphicsPreviewUpdateUIPatch
    {
        private static bool Prefix() => !GraphicsPreview.Applying;
    }

    /// <summary>OK : le jeu sauvegarde et applique, l'aperçu n'a plus rien à restaurer.</summary>
    [HarmonyPatch(typeof(GraphicsSettings), nameof(GraphicsSettings.OnOkAsync), new[] { typeof(OkActionCompletedHandler) })]
    internal static class GraphicsPreviewOkPatch
    {
        private static void Prefix() => GraphicsPreview.Confirm();
    }

    /// <summary>Fermeture de la fenêtre (Annuler, Échap, ou toute autre destruction) : restauration si pas d'OK.</summary>
    [HarmonyPatch(typeof(GraphicsSettings), nameof(GraphicsSettings.Terminate), new System.Type[0])]
    internal static class GraphicsPreviewTerminatePatch
    {
        private static void Postfix() => GraphicsPreview.End();
    }
}
