using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>
    /// Fabrique les lignes et en-têtes en clonant les modèles vanilla.
    /// Géométrie d'un modèle (onglet Accessibilité, cellule de GridLayoutGroup) : la racine est un point 0×0 d'où
    /// part le contrôle (case à cocher centrée dessus, barre du curseur de 300 px vers la droite) ; le libellé,
    /// 300 px de large, est accroché à sa gauche (pos −16, pivot droit) et le texte de valeur du curseur à droite
    /// de la barre. Un modèle posé tel quel comme enfant d'un VerticalLayoutGroup est étiré sur toute la largeur :
    /// le libellé finit à gauche de la page, la valeur à droite, tous deux rognés par le masque du Viewport.
    /// D'où un conteneur pleine largeur par ligne, avec le contrôle à une abscisse fixe et le libellé aligné à
    /// gauche depuis un retrait donné (0 pour un titre de section, plus pour les options qu'il chapeaute).
    /// </summary>
    internal static class SettingRows
    {
        public const float RowHeight = 30f;
        /// <summary>Abscisse du contrôle dans la ligne : le libellé (+ 16 px d'écart) tient à sa gauche.</summary>
        private const float ControlX = 400f;
        private const float ControlWidth = 300f;
        private const float ControlHeight = 20f;
        private const float LabelGap = 16f;
        private const float HeaderScale = 1.15f;

        /// <summary>Texte, titre d'infobulle et placement d'une ligne.</summary>
        private struct RowText
        {
            public string Text;
            public string TooltipTitle;
            public float Indent;
            public float Height;
        }

        public static SettingRow Create(ConfigEntryBase entry, SettingLabel label, RowTemplates templates, Transform parent,
            float indent)
        {
            var text = new RowText { Text = label.Display, TooltipTitle = label.Label, Indent = indent, Height = RowHeight };
            if (entry is ConfigEntry<bool> boolEntry)
                return CreateToggle(boolEntry, text, templates, parent, out _);
            if (entry.SettingType == typeof(float) || entry.SettingType == typeof(int))
                return CreateSlider(entry, label, text, templates, parent);
            Plugin.Log.LogWarning($"SettingsMenu : type non géré pour {entry.Definition} ({entry.SettingType.Name})");
            return null;
        }

        /// <summary>
        /// Titre de section portant la case « Activé » de la section : texte en gras à la taille d'un en-tête, case à
        /// la place habituelle, infobulle = titre + description de l'option. Suffixe de redémarrage en plus petit.
        /// </summary>
        public static ToggleRow CreateSectionToggle(ConfigEntry<bool> entry, SettingLabel label, string title,
            RowTemplates templates, Transform parent)
        {
            var display = label.RestartRequired ? $"<b>{title}</b> <size=85%>{SettingLabel.RestartSuffix}</size>" : $"<b>{title}</b>";
            var text = new RowText { Text = display, TooltipTitle = title, Indent = 0f, Height = HeaderHeight(templates.LabelSample) };
            var row = CreateToggle(entry, text, templates, parent, out var labelText);
            if (labelText != null && templates.LabelSample != null)
            {
                labelText.fontStyle = FontStyles.Normal;
                labelText.fontSize = templates.LabelSample.fontSize * HeaderScale;
            }
            return row;
        }

        /// <summary>Titre de section sans case (section sans option « Enabled »).</summary>
        public static void CreateHeader(Transform parent, TMP_Text sample, string title)
        {
            if (sample == null)
                return;
            var header = Object.Instantiate(sample, parent);
            header.name = OvomiumSettingsWindow.NamePrefix + "Header." + title;
            header.gameObject.SetActive(true);
            header.text = title;
            header.fontStyle = FontStyles.Bold;
            header.fontSize = sample.fontSize * HeaderScale;
            header.alignment = TextAlignmentOptions.MidlineLeft;
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = HeaderHeight(sample);
        }

        private static float HeaderHeight(TMP_Text sample) => sample == null ? RowHeight : sample.fontSize * HeaderScale + 16f;

        private static ToggleRow CreateToggle(ConfigEntry<bool> entry, RowText text, RowTemplates templates, Transform parent,
            out TMP_Text label)
        {
            var row = Clone(templates.ToggleRow, parent, entry, text, templates.Tooltip, out label);
            var toggle = row.GetComponentInChildren<Toggle>(true);
            if (toggle != null)
                return new ToggleRow(entry, toggle);
            Plugin.Log.LogWarning($"SettingsMenu : pas de Toggle dans le clone de « {templates.ToggleRow.name} »");
            return null;
        }

        private static SettingRow CreateSlider(ConfigEntryBase entry, SettingLabel label, RowText text, RowTemplates templates,
            Transform parent)
        {
            var row = Clone(templates.SliderRow, parent, entry, text, templates.Tooltip, out _);
            var slider = row.GetComponentInChildren<Slider>(true);
            if (slider == null)
            {
                Plugin.Log.LogWarning($"SettingsMenu : pas de Slider dans le clone de « {templates.SliderRow.name} »");
                return null;
            }
            if (label.LivePreview)
                slider.gameObject.AddComponent<SliderPeek>();
            return new SliderRow(entry, slider, FindOrCreateValueText(row.transform, templates));
        }

        /// <summary>Conteneur de ligne (pleine largeur, hauteur fixe) contenant le clone du modèle à <see cref="ControlX"/>.</summary>
        private static GameObject Clone(GameObject template, Transform parent, ConfigEntryBase entry, RowText text,
            GameObject tooltipPanel, out TMP_Text label)
        {
            var row = new GameObject(OvomiumSettingsWindow.NamePrefix + entry.Definition.Section + "." + entry.Definition.Key,
                typeof(RectTransform));
            row.transform.SetParent(parent, false);
            row.AddComponent<LayoutElement>().preferredHeight = text.Height;

            var control = Object.Instantiate(template, row.transform);
            control.SetActive(true);
            PlaceControl((RectTransform)control.transform);
            label = RowTemplates.FindLabel(control.transform);
            if (label != null)
            {
                label.text = text.Text;
                PlaceLabel(label, text.Indent);
            }
            AttachTooltip(control, text.TooltipTitle, entry, tooltipPanel);
            return row;
        }

        /// <summary>
        /// Infobulle vanilla : le clone pointe encore (m_tooltip) sur le panneau de la page Accessibilité, inactive ;
        /// on le redirige vers notre copie du panneau. Les curseurs n'en ont pas dans le prefab : on en ajoute une.
        /// </summary>
        private static void AttachTooltip(GameObject control, string title, ConfigEntryBase entry, GameObject panel)
        {
            if (panel == null)
                return;
            var tooltip = control.GetComponent<SettingsTooltip>() ?? control.AddComponent<SettingsTooltip>();
            tooltip.m_tooltip = panel;
            tooltip.SetTexts(title, entry.Description.Description);
        }

        private static void PlaceControl(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(ControlX, 0f);
            rect.sizeDelta = new Vector2(ControlWidth, ControlHeight);
        }

        /// <summary>
        /// Libellé aligné à gauche, de <paramref name="indent"/> (depuis le bord gauche de la ligne) jusqu'à 16 px du
        /// contrôle, sur toute la hauteur. Il est enfant du contrôle, d'où les coordonnées relatives à son bord gauche.
        /// </summary>
        private static void PlaceLabel(TMP_Text label, float indent)
        {
            label.alignment = TextAlignmentOptions.MidlineLeft;
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(indent - ControlX, 0f);
            rect.sizeDelta = new Vector2(ControlX - LabelGap - indent, 0f);
        }

        /// <summary>Texte de valeur du curseur : celui du modèle, sinon un texte créé à droite de la barre.</summary>
        private static TMP_Text FindOrCreateValueText(Transform row, RowTemplates templates)
        {
            var control = row.GetChild(0);
            if (templates.SliderValuePath != null)
            {
                var found = control.Find(templates.SliderValuePath);
                if (found != null && found.GetComponent<TMP_Text>() is TMP_Text text)
                    return text;
            }
            var label = RowTemplates.FindLabel(control);
            foreach (var t in control.GetComponentsInChildren<TMP_Text>(true))
                if (t != label)
                    return t;
            if (label == null)
                return null;
            var value = Object.Instantiate(label, control);
            value.name = "Value";
            value.alignment = TextAlignmentOptions.MidlineLeft;
            var rect = value.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(10f, 0f);
            rect.sizeDelta = new Vector2(90f, ControlHeight);
            return value;
        }
    }
}
