using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>
    /// Une section du .cfg dans une page : titre (portant la case « Activé » si la section a une option
    /// <c>Enabled</c> étiquetée, sinon texte seul), puis ses autres options en retrait dans un groupe estompé et
    /// inerte tant que la section est désactivée, les options avancées repliées sous <see cref="AdvancedFold"/>.
    /// Hiérarchie : Content → [titre, Options (VerticalLayoutGroup + CanvasGroup) → [lignes, pli, Advanced → [lignes]]].
    /// </summary>
    internal sealed class SettingSection
    {
        private const string EnabledKey = "Enabled";
        private const float Indent = 24f;
        private const float AdvancedIndent = 48f;
        private const float DisabledAlpha = 0.4f;
        private const float Spacing = 4f;

        private ToggleRow m_enabled;
        private CanvasGroup m_options;

        /// <summary>Construit la section sous <paramref name="content"/> ; null si elle n'a aucune option étiquetée.</summary>
        public static SettingSection Build(string section, ConfigFile config, RowTemplates templates, Transform content,
            List<SettingRow> rows)
        {
            var entries = EntriesOf(config, section);
            if (entries.Count == 0)
                return null;
            var built = new SettingSection();
            var title = SettingsMenuLayout.SectionLabel(section);
            if (entries[0].Definition.Key == EnabledKey && entries[0] is ConfigEntry<bool> enabled)
            {
                built.m_enabled = SettingRows.CreateSectionToggle(enabled, SettingLabel.Of(enabled), title, templates, content);
                entries.RemoveAt(0);
            }
            if (built.m_enabled != null)
            {
                rows.Add(built.m_enabled);
                built.m_enabled.Changed += _ => built.Refresh();
            }
            else
                SettingRows.CreateHeader(content, templates.LabelSample, title);
            if (entries.Count > 0)
                built.m_options = BuildOptions(section, entries, templates, content, rows);
            return built;
        }

        /// <summary>Options taguées de la section : <c>Enabled</c> d'abord, puis l'ordre de déclaration (ordre de Bind).</summary>
        private static List<ConfigEntryBase> EntriesOf(ConfigFile config, string section)
        {
            var entries = new List<ConfigEntryBase>();
            foreach (var pair in config)
            {
                if (pair.Key.Section != section || SettingLabel.Of(pair.Value) == null)
                    continue;
                if (pair.Key.Key == EnabledKey)
                    entries.Insert(0, pair.Value);
                else
                    entries.Add(pair.Value);
            }
            return entries;
        }

        private static CanvasGroup BuildOptions(string section, List<ConfigEntryBase> entries, RowTemplates templates,
            Transform content, List<SettingRow> rows)
        {
            var options = CreateGroup(section + ".Options", content);
            GameObject advanced = null;
            foreach (var entry in entries)
            {
                var label = SettingLabel.Of(entry);
                if (label.Advanced && advanced == null)
                    advanced = CreateGroup(section + ".Advanced", options.transform);
                var parent = label.Advanced ? advanced.transform : options.transform;
                var row = SettingRows.Create(entry, label, templates, parent, label.Advanced ? AdvancedIndent : Indent);
                if (row != null)
                    rows.Add(row);
            }
            var group = options.AddComponent<CanvasGroup>();
            if (advanced != null && templates.LabelSample != null)
                AdvancedFold.Create(advanced, templates.LabelSample, Indent, group);
            return group;
        }

        /// <summary>
        /// Liste verticale imbriquée : le VerticalLayoutGroup parent lit sa hauteur préférée, et la recalcule quand
        /// on l'active ou le désactive (repli), ce que le ContentSizeFitter du contenu et le ScrollRect suivent.
        /// </summary>
        private static GameObject CreateGroup(string name, Transform parent)
        {
            var group = new GameObject(OvomiumSettingsWindow.NamePrefix + name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            group.transform.SetParent(parent, false);
            var layout = group.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = Spacing;
            return group;
        }

        /// <summary>Estompe et fige les options si la case « Activé » de la section est décochée.</summary>
        public void Refresh()
        {
            if (m_enabled == null || m_options == null)
                return;
            var on = m_enabled.IsOn;
            m_options.alpha = on ? 1f : DisabledAlpha;
            m_options.interactable = on;
        }
    }
}
