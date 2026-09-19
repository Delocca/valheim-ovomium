using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>
    /// Une page de la fenêtre Ovomium (composant <c>ISettingsTab</c> attendu par <c>Settings</c> sur chaque page) :
    /// liste défilante d'en-têtes de section et de lignes générées depuis les options taguées <see cref="SettingLabel"/>.
    /// </summary>
    internal sealed class OvomiumSettingsTab : MonoBehaviour, ISettingsTab
    {
        private readonly List<SettingRow> m_rows = new List<SettingRow>();

#pragma warning disable CS0067 // exigé par l'interface, jamais levé ici
        public event System.Action<string, int> SharedSettingChanged;
#pragma warning restore CS0067

        /// <summary>Crée la page (même rect que <paramref name="templatePage"/>) et la remplit.</summary>
        public static OvomiumSettingsTab CreatePage(RectTransform templatePage, SettingsMenuLayout.Tab layout,
            RowTemplates templates, ConfigFile config)
        {
            var page = new GameObject(OvomiumSettingsWindow.NamePrefix + layout.Title, typeof(RectTransform));
            var rect = (RectTransform)page.transform;
            rect.SetParent(templatePage.parent, false);
            rect.SetSiblingIndex(templatePage.GetSiblingIndex() + 1);
            CopyRect(templatePage, rect);
            var content = BuildScrollView(rect, templates.Scrollbar);
            var tab = page.AddComponent<OvomiumSettingsTab>();
            tab.Fill(content, layout, templates, config);
            page.SetActive(false);
            return tab;
        }

        private static void CopyRect(RectTransform from, RectTransform to)
        {
            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition;
            to.sizeDelta = from.sizeDelta;
        }

        /// <summary>
        /// Page (ScrollRect) → Viewport (masque, image transparente pour recevoir la molette entre les lignes) →
        /// Content (liste verticale ajustée à son contenu) ; barre vanilla clonée à droite, visible seulement si ça dépasse.
        /// </summary>
        private static RectTransform BuildScrollView(RectTransform page, Scrollbar scrollbarTemplate)
        {
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.GetComponent<Image>().color = Color.clear;
            var viewRect = (RectTransform)viewport.transform;
            viewRect.SetParent(page, false);
            Stretch(viewRect);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var contentRect = (RectTransform)content.transform;
            contentRect.SetParent(viewRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = page.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 600f; // vingt lignes par cran de molette (réglage d'Edia)
            AttachScrollbar(scroll, page, scrollbarTemplate);
            return contentRect;
        }

        /// <summary>Barre clonée sur le bord droit de la page ; Unity la masque et rend sa largeur au viewport quand tout tient.</summary>
        private static void AttachScrollbar(ScrollRect scroll, RectTransform page, Scrollbar template)
        {
            if (template == null)
                return;
            var width = ((RectTransform)template.transform).rect.width;
            var bar = Instantiate(template, page);
            bar.name = "Scrollbar";
            bar.gameObject.SetActive(true);
            bar.direction = Scrollbar.Direction.BottomToTop;
            var rect = (RectTransform)bar.transform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width > 0f ? width : 12f, 0f);
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 4f;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void Fill(Transform content, SettingsMenuLayout.Tab layout, RowTemplates templates, ConfigFile config)
        {
            foreach (var section in layout.Sections)
            {
                SettingRows.CreateHeader(content, templates.LabelSample, SettingsMenuLayout.SectionLabel(section));
                foreach (var entry in EntriesOf(config, section))
                {
                    var row = SettingRows.Create(entry, LabelOf(entry), templates, content);
                    if (row != null)
                        m_rows.Add(row);
                }
            }
        }

        /// <summary>Options taguées d'une section, dans l'ordre de déclaration (ordre de Bind).</summary>
        private static IEnumerable<ConfigEntryBase> EntriesOf(ConfigFile config, string section)
        {
            foreach (var pair in config)
                if (pair.Key.Section == section && LabelOf(pair.Value) != null)
                    yield return pair.Value;
        }

        private static SettingLabel LabelOf(ConfigEntryBase entry)
        {
            foreach (var tag in entry.Description.Tags)
                if (tag is SettingLabel label)
                    return label;
            return null;
        }

        /// <summary>
        /// Les lignes appliquent chaque changement aussitôt (aperçu en direct) ; l'écriture du fichier cfg est
        /// suspendue jusqu'au OK pour ne pas écrire à chaque cran de curseur. Retour remet les valeurs d'ouverture.
        /// </summary>
        public void Initialize()
        {
            Plugin.ConfigFile.SaveOnConfigSet = false;
            foreach (var row in m_rows)
                row.Load();
        }

        // Membres à implémentation par défaut dans ISettingsTab : redéclarés, le compilateur net48 refuse d'en hériter.
        public void Terminate() { }

        public void OnBack()
        {
            foreach (var row in m_rows)
                row.Revert();
            Plugin.ConfigFile.SaveOnConfigSet = true;
        }
        public void OnSharedSettingChanged(string setting, int value) { }
        public void OnTabOpen(Button backButton, Button okButton)
        {
            if (SettingsMenuConfig.DumpHierarchy.Value)
                Plugin.Log.LogInfo($"SettingsMenu : onglet {name} ouvert, {m_rows.Count} ligne(s), actif={gameObject.activeInHierarchy}");
        }

        public void OnOkAsync(OkActionCompletedHandler okActionCompletedCallback)
        {
            Plugin.ConfigFile.SaveOnConfigSet = true;
            Plugin.ConfigFile.Save();
            okActionCompletedCallback?.Invoke();
        }
    }
}
