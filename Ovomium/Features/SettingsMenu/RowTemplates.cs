using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>
    /// Lignes vanilla à cloner pour garder le style du jeu : une ligne bascule et une ligne curseur de l'onglet
    /// Accessibilité (champs sérialisés publicisés, plus fiables que des noms d'objets).
    /// </summary>
    internal sealed class RowTemplates
    {
        public GameObject ToggleRow;
        public GameObject SliderRow;
        /// <summary>Chemin du texte de valeur dans <see cref="SliderRow"/>, null s'il est ailleurs.</summary>
        public string SliderValuePath;
        /// <summary>Texte de référence (police, matériau) pour les en-têtes de section.</summary>
        public TMP_Text LabelSample;

        public static RowTemplates FromVanilla(Settings settings)
        {
            var page = settings.GetComponentInChildren<AccessibilitySettings>(true);
            if (page == null)
            {
                Plugin.Log.LogWarning("SettingsMenu : onglet Accessibilité introuvable dans le prefab Paramètres");
                return null;
            }
            if (page.m_toggleRun == null || page.m_guiScaleSlider == null)
            {
                Plugin.Log.LogWarning("SettingsMenu : m_toggleRun ou m_guiScaleSlider absent dans AccessibilitySettings");
                return null;
            }
            var t = new RowTemplates
            {
                ToggleRow = ClimbToRow(page.m_toggleRun.transform, page.transform),
                SliderRow = ClimbToRow(page.m_guiScaleSlider.transform, page.transform),
            };
            t.SliderValuePath = page.m_guiScaleText == null ? null : RelativePath(page.m_guiScaleText.transform, t.SliderRow.transform);
            t.LabelSample = FindLabel(t.ToggleRow.transform);
            if (t.LabelSample == null)
                Plugin.Log.LogWarning($"SettingsMenu : aucun TMP_Text dans la ligne bascule « {t.ToggleRow.name} »");
            return t;
        }

        /// <summary>Remonte du contrôle vers la « ligne » : le plus haut ancêtre (sous la page) ne contenant qu'un seul contrôle.</summary>
        private static GameObject ClimbToRow(Transform control, Transform page)
        {
            var row = control;
            while (row.parent != null && row.parent != page
                   && row.parent.GetComponentsInChildren<Selectable>(true).Length == 1)
                row = row.parent;
            return row.gameObject;
        }

        private static string RelativePath(Transform target, Transform root)
        {
            var path = "";
            for (var t = target; t != null; t = t.parent)
            {
                if (t == root)
                    return path;
                path = path.Length == 0 ? t.name : t.name + "/" + path;
            }
            return null;
        }

        /// <summary>Le TMP_Text « Label » d'une ligne, sinon le premier trouvé.</summary>
        public static TMP_Text FindLabel(Transform row)
        {
            var texts = row.GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in texts)
                if (text.name.ToLowerInvariant().Contains("label"))
                    return text;
            return texts.Length > 0 ? texts[0] : null;
        }
    }
}
