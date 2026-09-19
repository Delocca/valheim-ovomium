using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>
    /// Une ligne de la fenêtre Ovomium : lit son option à l'ouverture, l'écrit à chaque changement (les features
    /// lisent leur config en continu, l'effet est donc immédiat) et la remet à sa valeur d'ouverture sur Retour.
    /// </summary>
    internal abstract class SettingRow
    {
        private object m_original;

        protected abstract ConfigEntryBase Entry { get; }
        /// <summary>Affiche la valeur courante de l'option dans le contrôle.</summary>
        protected abstract void Show();

        public void Load()
        {
            m_original = Entry.BoxedValue;
            Show();
        }

        public void Revert() => Apply(m_original);

        protected void Apply(object value)
        {
            if (!value.Equals(Entry.BoxedValue))
                Entry.BoxedValue = value;
        }
    }

    internal sealed class ToggleRow : SettingRow
    {
        private readonly ConfigEntry<bool> m_entry;
        private readonly Toggle m_toggle;

        protected override ConfigEntryBase Entry => m_entry;

        public ToggleRow(ConfigEntry<bool> entry, Toggle toggle)
        {
            m_entry = entry;
            m_toggle = toggle;
            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.onValueChanged.AddListener(on => Apply(on));
        }

        protected override void Show() => m_toggle.isOn = m_entry.Value;
    }

    internal sealed class SliderRow : SettingRow
    {
        private readonly ConfigEntryBase m_entry;
        private readonly Slider m_slider;
        private readonly TMP_Text m_value;
        private readonly bool m_isInt;

        protected override ConfigEntryBase Entry => m_entry;

        public SliderRow(ConfigEntryBase entry, Slider slider, TMP_Text value)
        {
            m_entry = entry;
            m_slider = slider;
            m_value = value;
            m_isInt = entry.SettingType == typeof(int);
            slider.onValueChanged = new Slider.SliderEvent();
            slider.wholeNumbers = m_isInt;
            ApplyRange();
            slider.onValueChanged.AddListener(_ => OnChanged());
        }

        private void ApplyRange()
        {
            switch (m_entry.Description.AcceptableValues)
            {
                case AcceptableValueRange<float> f:
                    m_slider.minValue = f.MinValue;
                    m_slider.maxValue = f.MaxValue;
                    break;
                case AcceptableValueRange<int> i:
                    m_slider.minValue = i.MinValue;
                    m_slider.maxValue = i.MaxValue;
                    break;
                default:
                    Plugin.Log.LogWarning($"SettingsMenu : {m_entry.Definition} sans AcceptableValueRange, curseur 0–1");
                    m_slider.minValue = 0f;
                    m_slider.maxValue = 1f;
                    break;
            }
        }

        protected override void Show()
        {
            m_slider.value = m_isInt ? (int)m_entry.BoxedValue : (float)m_entry.BoxedValue;
            ShowValue();
        }

        private void OnChanged()
        {
            ShowValue();
            Apply(m_isInt ? (object)Mathf.RoundToInt(m_slider.value) : m_slider.value);
        }

        private void ShowValue()
        {
            if (m_value != null)
                m_value.text = m_isInt ? Mathf.RoundToInt(m_slider.value).ToString() : m_slider.value.ToString("G3");
        }
    }

    /// <summary>
    /// Fabrique les lignes et en-têtes en clonant les modèles vanilla.
    /// Géométrie d'un modèle (onglet Accessibilité, cellule de GridLayoutGroup) : la racine est un point 0×0 d'où
    /// part le contrôle (case à cocher centrée dessus, barre du curseur de 300 px vers la droite) ; le libellé,
    /// 300 px de large, est accroché à sa gauche (pos −16, pivot droit) et le texte de valeur du curseur à droite
    /// de la barre. Un modèle posé tel quel comme enfant d'un VerticalLayoutGroup est étiré sur toute la largeur :
    /// le libellé finit à gauche de la page, la valeur à droite, tous deux rognés par le masque du Viewport.
    /// D'où un conteneur pleine largeur par ligne, avec le contrôle à une abscisse fixe.
    /// </summary>
    internal static class SettingRows
    {
        private const float RowHeight = 30f;
        /// <summary>Abscisse du contrôle dans la ligne : le libellé (300 px + 16 px d'écart) tient à sa gauche.</summary>
        private const float ControlX = 400f;
        private const float ControlWidth = 300f;
        private const float ControlHeight = 20f;
        private const float LabelWidth = 300f;
        private const float LabelGap = 16f;

        public static SettingRow Create(ConfigEntryBase entry, SettingLabel label, RowTemplates templates, Transform parent)
        {
            if (entry is ConfigEntry<bool> boolEntry)
            {
                var row = Clone(templates.ToggleRow, parent, entry, label, templates.Tooltip);
                var toggle = row.GetComponentInChildren<Toggle>(true);
                if (toggle != null)
                    return new ToggleRow(boolEntry, toggle);
                Plugin.Log.LogWarning($"SettingsMenu : pas de Toggle dans le clone de « {templates.ToggleRow.name} »");
            }
            else if (entry.SettingType == typeof(float) || entry.SettingType == typeof(int))
            {
                var row = Clone(templates.SliderRow, parent, entry, label, templates.Tooltip);
                var slider = row.GetComponentInChildren<Slider>(true);
                if (slider != null)
                    return new SliderRow(entry, slider, FindOrCreateValueText(row.transform, templates));
                Plugin.Log.LogWarning($"SettingsMenu : pas de Slider dans le clone de « {templates.SliderRow.name} »");
            }
            else
                Plugin.Log.LogWarning($"SettingsMenu : type non géré pour {entry.Definition} ({entry.SettingType.Name})");
            return null;
        }

        /// <summary>Conteneur de ligne (pleine largeur, hauteur fixe) contenant le clone du modèle à <see cref="ControlX"/>.</summary>
        private static GameObject Clone(GameObject template, Transform parent, ConfigEntryBase entry, SettingLabel label,
            GameObject tooltipPanel)
        {
            var row = new GameObject("Ovomium." + entry.Definition.Section + "." + entry.Definition.Key, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            row.AddComponent<LayoutElement>().preferredHeight = RowHeight;

            var control = Object.Instantiate(template, row.transform);
            control.SetActive(true);
            PlaceControl((RectTransform)control.transform);
            var text = RowTemplates.FindLabel(control.transform);
            if (text != null)
            {
                text.text = label.Display;
                PlaceLabel(text.rectTransform);
            }
            AttachTooltip(control, label, entry, tooltipPanel);
            return row;
        }

        /// <summary>
        /// Infobulle vanilla : le clone pointe encore (m_tooltip) sur le panneau de la page Accessibilité, inactive ;
        /// on le redirige vers notre copie du panneau. Les curseurs n'en ont pas dans le prefab : on en ajoute une.
        /// </summary>
        private static void AttachTooltip(GameObject control, SettingLabel label, ConfigEntryBase entry, GameObject panel)
        {
            if (panel == null)
                return;
            var tooltip = control.GetComponent<SettingsTooltip>() ?? control.AddComponent<SettingsTooltip>();
            tooltip.m_tooltip = panel;
            tooltip.SetTexts(label.Label, entry.Description.Description);
        }

        private static void PlaceControl(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(ControlX, 0f);
            rect.sizeDelta = new Vector2(ControlWidth, ControlHeight);
        }

        /// <summary>Libellé à gauche du contrôle, sur toute la hauteur de la ligne (géométrie vanilla, fixée explicitement).</summary>
        private static void PlaceLabel(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-LabelGap, 0f);
            rect.sizeDelta = new Vector2(LabelWidth, 0f);
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

        public static void CreateHeader(Transform parent, TMP_Text sample, string title)
        {
            if (sample == null)
                return;
            var header = Object.Instantiate(sample, parent);
            header.name = "Ovomium.Header." + title;
            header.gameObject.SetActive(true);
            header.text = title;
            header.fontStyle = FontStyles.Bold;
            header.fontSize = sample.fontSize * 1.15f;
            header.alignment = TextAlignmentOptions.MidlineLeft;
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = header.fontSize + 16f;
        }
    }
}
