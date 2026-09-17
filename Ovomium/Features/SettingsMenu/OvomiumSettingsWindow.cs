using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>
    /// Fenêtre Ovomium = prefab Paramètres vanilla (cadre, boutons Appliquer/Retour, onglets) dont les onglets du jeu
    /// sont remplacés par les nôtres dans <c>Settings.Awake</c>, quand l'ouverture vient de nos boutons.
    /// </summary>
    internal static class OvomiumSettingsWindow
    {
        private static bool s_pending;

        /// <summary>Même séquence que <c>Menu.OnSettings</c>, pour que le menu Échap gère la fermeture pareil.</summary>
        public static void OpenFromMenu(Menu menu)
        {
            s_pending = true;
            menu.m_settingsInstance = Object.Instantiate(menu.m_settingsPrefab, menu.transform);
            menu.m_closeMenuState = Menu.CloseMenuState.SettingsOpen;
            s_pending = false;
        }

        /// <summary>Même séquence que <c>FejdStartup.OnButtonSettings</c>.</summary>
        public static void OpenFromMainMenu(FejdStartup startup)
        {
            startup.m_mainMenu.SetActive(false);
            s_pending = true;
            startup.m_settingsPopup = Object.Instantiate(startup.m_settingsPrefab, startup.transform);
            s_pending = false;
            var settings = startup.m_settingsPopup.GetComponent<Settings>();
            settings.SettingsClosed += () =>
            {
                if (startup.m_mainMenu != null)
                    startup.m_mainMenu.SetActive(true);
            };
        }

        /// <summary>Prefix de <c>Settings.Awake</c> : avant <c>InitializeTabs</c>, qui lit <c>TabHandler.m_tabs</c>.</summary>
        public static void OnSettingsAwake(Settings settings)
        {
            if (!s_pending)
                return;
            s_pending = false;
            if (SettingsMenuConfig.DumpHierarchy.Value)
                HierarchyDump.Log(settings.transform);
            var tabHandler = settings.GetComponentInChildren<TabHandler>(true);
            if (tabHandler == null || tabHandler.m_tabs.Count == 0)
            {
                Plugin.Log.LogWarning("SettingsMenu : TabHandler ou onglets vanilla introuvables, fenêtre vanilla conservée");
                return;
            }
            var templates = RowTemplates.FromVanilla(settings);
            if (templates == null)
                return;
            ReplaceTabs(tabHandler, templates);
        }

        private static void ReplaceTabs(TabHandler tabHandler, RowTemplates templates)
        {
            var vanilla = new List<TabHandler.Tab>(tabHandler.m_tabs);
            var templateButton = vanilla[0].m_button;
            var templatePage = PageOf<Valheim.SettingsGui.AccessibilitySettings>(vanilla) ?? vanilla[0].m_page;
            tabHandler.m_tabs.Clear();
            var buttonIndex = vanilla[vanilla.Count - 1].m_button.transform.GetSiblingIndex();
            foreach (var layout in SettingsMenuLayout.Tabs)
            {
                var page = OvomiumSettingsTab.CreatePage(templatePage, layout, templates, Plugin.ConfigFile);
                var button = CloneTabButton(templateButton, layout.Title, ++buttonIndex);
                tabHandler.m_tabs.Add(new TabHandler.Tab
                {
                    m_button = button,
                    m_page = (RectTransform)page.transform,
                    m_default = tabHandler.m_tabs.Count == 0,
                    m_onClick = new UnityEvent(),
                });
            }
            // Après le clonage : les modèles doivent rester actifs pendant Instantiate.
            foreach (var tab in vanilla)
            {
                if (tab.m_button != null)
                    tab.m_button.gameObject.SetActive(false);
                if (tab.m_page != null)
                    tab.m_page.gameObject.SetActive(false);
            }
        }

        private static RectTransform PageOf<T>(List<TabHandler.Tab> tabs) where T : Component
        {
            foreach (var tab in tabs)
                if (tab.m_page != null && tab.m_page.GetComponent<T>() != null)
                    return tab.m_page;
            return null;
        }

        private static Button CloneTabButton(Button template, string title, int siblingIndex)
        {
            var button = Object.Instantiate(template, template.transform.parent);
            button.name = "Ovomium.Tab." + title;
            button.transform.SetSiblingIndex(siblingIndex);
            button.gameObject.SetActive(true);
            button.interactable = true;
            button.onClick = new Button.ButtonClickedEvent();
            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
                text.text = title;
            return button;
        }
    }
}
