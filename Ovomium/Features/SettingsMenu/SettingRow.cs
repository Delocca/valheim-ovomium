using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        /// <summary>État affiché par la case (levé après l'écriture de l'option).</summary>
        public event System.Action<bool> Changed;
        public bool IsOn => m_toggle.isOn;

        public ToggleRow(ConfigEntry<bool> entry, Toggle toggle)
        {
            m_entry = entry;
            m_toggle = toggle;
            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.onValueChanged.AddListener(on =>
            {
                Apply(on);
                Changed?.Invoke(on);
            });
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
}
