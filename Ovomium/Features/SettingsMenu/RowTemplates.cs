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
        /// <summary>Panneau d'infobulle (copie de celui de la page Accessibilité, sous TabContent), null si absent.</summary>
        public GameObject Tooltip;
        /// <summary>Barre de défilement verticale vanilla à cloner, null si absente.</summary>
        public Scrollbar Scrollbar;

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
            t.Tooltip = CloneTooltipPanel(page.transform);
            t.Scrollbar = FindScrollbar(settings);
            return t;
        }

        /// <summary>Barre de la liste défilante de la page Graphismes, sinon celle de sa liste de résolutions (dialogue inactif).</summary>
        private static Scrollbar FindScrollbar(Settings settings)
        {
            var graphics = settings.GetComponentInChildren<GraphicsSettings>(true);
            if (graphics == null)
            {
                Plugin.Log.LogWarning("SettingsMenu : onglet Graphismes introuvable, pages sans barre de défilement");
                return null;
            }
            var list = graphics.m_settingsListScrollRectEnsureVisible == null
                ? null : graphics.m_settingsListScrollRectEnsureVisible.GetComponent<ScrollRect>();
            var fromList = list != null && list.verticalScrollbar != null;
            var bar = fromList ? list.verticalScrollbar : graphics.m_resolutionListScroll;
            if (bar == null)
                Plugin.Log.LogWarning("SettingsMenu : aucune barre de défilement dans GraphicsSettings, pages sans barre");
            else if (SettingsMenuConfig.DumpHierarchy.Value)
                Plugin.Log.LogInfo($"SettingsMenu : barre de défilement modèle « {bar.name} » ({(fromList ? "liste" : "résolutions")})");
            return bar;
        }

        /// <summary>Le panneau « SettingsTooltip » de la page, copié sous le parent des pages (toujours actif), au-dessus d'elles.</summary>
        private static GameObject CloneTooltipPanel(Transform page)
        {
            var panel = page.Find("SettingsTooltip");
            if (panel == null)
            {
                Plugin.Log.LogWarning("SettingsMenu : panneau SettingsTooltip absent de la page Accessibilité, pas d'infobulles");
                return null;
            }
            var clone = Object.Instantiate(panel.gameObject, page.parent);
            clone.name = OvomiumSettingsWindow.NamePrefix + "SettingsTooltip";
            clone.transform.SetAsLastSibling();
            clone.SetActive(false);
            return clone;
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
