using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>Une ligne de la fenêtre Ovomium : lit son option à l'ouverture, l'écrit à Appliquer.</summary>
    internal abstract class SettingRow
    {
        public abstract void Load();
        public abstract void Save();
    }

    internal sealed class ToggleRow : SettingRow
    {
        private readonly ConfigEntry<bool> m_entry;
        private readonly Toggle m_toggle;

        public ToggleRow(ConfigEntry<bool> entry, Toggle toggle)
        {
            m_entry = entry;
            m_toggle = toggle;
            toggle.onValueChanged = new Toggle.ToggleEvent();
        }

        public override void Load() => m_toggle.isOn = m_entry.Value;

        public override void Save()
        {
            if (m_toggle.isOn != m_entry.Value)
                m_entry.Value = m_toggle.isOn;
        }
    }

    internal sealed class SliderRow : SettingRow
    {
        private readonly ConfigEntryBase m_entry;
        private readonly Slider m_slider;
        private readonly TMP_Text m_value;
        private readonly bool m_isInt;

        public SliderRow(ConfigEntryBase entry, Slider slider, TMP_Text value)
        {
            m_entry = entry;
            m_slider = slider;
            m_value = value;
            m_isInt = entry.SettingType == typeof(int);
            slider.onValueChanged = new Slider.SliderEvent();
            slider.wholeNumbers = m_isInt;
            ApplyRange();
            slider.onValueChanged.AddListener(_ => ShowValue());
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

        public override void Load()
        {
            m_slider.value = m_isInt ? (int)m_entry.BoxedValue : (float)m_entry.BoxedValue;
            ShowValue();
        }

        public override void Save()
        {
            object value = m_isInt ? (object)Mathf.RoundToInt(m_slider.value) : m_slider.value;
            if (!value.Equals(m_entry.BoxedValue))
                m_entry.BoxedValue = value;
        }

        private void ShowValue()
        {
            if (m_value != null)
                m_value.text = m_isInt ? Mathf.RoundToInt(m_slider.value).ToString() : m_slider.value.ToString("G3");
        }
    }

    /// <summary>Fabrique les lignes et en-têtes en clonant les modèles vanilla.</summary>
    internal static class SettingRows
    {
        private const float MinRowHeight = 30f;

        public static SettingRow Create(ConfigEntryBase entry, SettingLabel label, RowTemplates templates, Transform parent)
        {
            if (entry is ConfigEntry<bool> boolEntry)
            {
                var row = Clone(templates.ToggleRow, parent, entry, label);
                var toggle = row.GetComponentInChildren<Toggle>(true);
                if (toggle != null)
                    return new ToggleRow(boolEntry, toggle);
                Plugin.Log.LogWarning($"SettingsMenu : pas de Toggle dans le clone de « {templates.ToggleRow.name} »");
            }
            else if (entry.SettingType == typeof(float) || entry.SettingType == typeof(int))
            {
                var row = Clone(templates.SliderRow, parent, entry, label);
                var slider = row.GetComponentInChildren<Slider>(true);
                if (slider != null)
                    return new SliderRow(entry, slider, FindOrCreateValueText(row.transform, templates));
                Plugin.Log.LogWarning($"SettingsMenu : pas de Slider dans le clone de « {templates.SliderRow.name} »");
            }
            else
                Plugin.Log.LogWarning($"SettingsMenu : type non géré pour {entry.Definition} ({entry.SettingType.Name})");
            return null;
        }

        private static GameObject Clone(GameObject template, Transform parent, ConfigEntryBase entry, SettingLabel label)
        {
            var row = Object.Instantiate(template, parent);
            row.name = "Ovomium." + entry.Definition.Section + "." + entry.Definition.Key;
            row.SetActive(true);
            var text = RowTemplates.FindLabel(row.transform);
            if (text != null)
                text.text = label.Display;
            foreach (var tooltip in row.GetComponentsInChildren<SettingsTooltip>(true))
                tooltip.SetTexts(label.Label, entry.Description.Description);
            AddLayoutHeight(row, MinRowHeight);
            return row;
        }

        private static TMP_Text FindOrCreateValueText(Transform row, RowTemplates templates)
        {
            if (templates.SliderValuePath != null)
            {
                var found = row.Find(templates.SliderValuePath);
                if (found != null && found.GetComponent<TMP_Text>() is TMP_Text text)
                    return text;
            }
            var label = RowTemplates.FindLabel(row);
            foreach (var t in row.GetComponentsInChildren<TMP_Text>(true))
                if (t != label)
                    return t;
            if (label == null)
                return null;
            var value = Object.Instantiate(label, row);
            value.name = "Value";
            value.alignment = TextAlignmentOptions.MidlineRight;
            var rect = value.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(90f, 0f);
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
            AddLayoutHeight(header.gameObject, sample.fontSize * 1.15f + 16f);
        }

        private static void AddLayoutHeight(GameObject go, float min)
        {
            var element = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            var height = ((RectTransform)go.transform).rect.height;
            element.preferredHeight = Mathf.Max(height, min);
        }
    }
}
