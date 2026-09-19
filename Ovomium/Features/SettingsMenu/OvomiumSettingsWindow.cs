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
        /// <summary>Préfixe des objets que nous posons dans l'instance du prefab : signe une fenêtre Ovomium par nom.</summary>
        public const string NamePrefix = "Ovomium.";

        /// <summary>
        /// Déchargement / rechargement à chaud : ferme une fenêtre Ovomium ouverte (par le même chemin que son bouton
        /// Retour), ses pages viennent de l'ancienne assembly. Reconnue par nom, jamais par type de composant du mod.
        /// </summary>
        public static void CloseAll()
        {
            if (Menu.instance != null)
                CloseIfOurs(Menu.instance.m_settingsInstance);
            if (FejdStartup.instance != null)
                CloseIfOurs(FejdStartup.instance.m_settingsPopup);
        }

        private static void CloseIfOurs(GameObject window)
        {
            if (window == null)
                return;
            var settings = window.GetComponent<Settings>();
            if (settings == null || !IsOvomiumWindow(window.transform))
                return;
            if (SettingsMenuConfig.DumpHierarchy.Value)
                Plugin.Log.LogInfo("SettingsMenu : fermeture de la fenêtre Ovomium au déchargement");
            settings.OnBack();
        }

        private static bool IsOvomiumWindow(Transform window)
        {
            foreach (var t in window.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(NamePrefix, System.StringComparison.Ordinal))
                    return true;
            return false;
        }

        /// <summary>Même séquence que <c>Menu.OnSettings</c>, pour que le menu Échap gère la fermeture pareil.</summary>
        public static void OpenFromMenu(Menu menu)
        {
            if (SettingsMenuConfig.DumpHierarchy.Value)
                Plugin.Log.LogInfo("SettingsMenu : ouverture depuis le menu Échap");
            s_pending = true;
            menu.m_settingsInstance = Object.Instantiate(menu.m_settingsPrefab, menu.transform);
            menu.m_closeMenuState = Menu.CloseMenuState.SettingsOpen;
            s_pending = false;
        }

        /// <summary>Même séquence que <c>FejdStartup.OnButtonSettings</c>.</summary>
        public static void OpenFromMainMenu(FejdStartup startup)
        {
            if (SettingsMenuConfig.DumpHierarchy.Value)
                Plugin.Log.LogInfo("SettingsMenu : ouverture depuis le menu principal");
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
            // Même recherche que Settings.InitializeTabs : sans les inactifs, sinon on tombe sur le TabHandler des
            // sous-onglets Manette/Souris de la page Gamepad (inactive), qui précède la barre principale.
            var tabHandler = settings.GetComponentInChildren<TabHandler>();
            if (tabHandler == null || tabHandler.m_tabs.Count == 0)
            {
                Plugin.Log.LogWarning("SettingsMenu : TabHandler ou onglets vanilla introuvables, fenêtre vanilla conservée");
                return;
            }
            var templates = RowTemplates.FromVanilla(settings);
            if (templates == null)
                return;
            ReplaceTabs(tabHandler, templates);
            if (SettingsMenuConfig.DumpHierarchy.Value)
            {
                LogTabs("après remplacement", tabHandler);
                HierarchyDump.Log(tabHandler.m_tabs[0].m_page, 8);
                HierarchyDump.Log(templates.ToggleRow.transform, 8);
            }
        }

        /// <summary>Trace de diagnostic : état de chaque onglet du TabHandler et de ses boutons frères.</summary>
        public static void LogTabs(string moment, TabHandler tabHandler)
        {
            var sb = new System.Text.StringBuilder("SettingsMenu : onglets ").Append(moment).Append(" : ");
            foreach (var tab in tabHandler.m_tabs)
                sb.Append(tab.m_button ? tab.m_button.name : "?").Append(tab.m_button && tab.m_button.gameObject.activeSelf ? "" : "(bouton inactif)")
                  .Append(" → ").Append(tab.m_page ? tab.m_page.name : "?").Append(tab.m_page && tab.m_page.gameObject.activeSelf ? "" : "(page inactive)").Append(" ; ");
            sb.Append("boutons frères : ");
            foreach (Transform child in tabHandler.transform)
                sb.Append(child.name).Append(child.gameObject.activeSelf ? " " : "(inactif) ");
            Plugin.Log.LogInfo(sb.ToString());
        }

        private static void ReplaceTabs(TabHandler tabHandler, RowTemplates templates)
        {
            var vanilla = new List<TabHandler.Tab>(tabHandler.m_tabs);
            // Certains onglets n'ont pas de bouton (RadialTab, ouvert depuis la page Manette) : ne garder que les boutonnés.
            var buttons = vanilla.FindAll(t => t.m_button != null);
            if (buttons.Count == 0)
            {
                Plugin.Log.LogWarning("SettingsMenu : aucun onglet vanilla avec bouton, fenêtre vanilla conservée");
                return;
            }
            var templateButton = buttons[0].m_button;
            var templatePage = PageOf<Valheim.SettingsGui.AccessibilitySettings>(vanilla) ?? buttons[0].m_page;
            var buttonIndex = buttons[buttons.Count - 1].m_button.transform.GetSiblingIndex();
            tabHandler.m_tabs.Clear();
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
            button.name = NamePrefix + "Tab." + title;
            button.transform.SetSiblingIndex(siblingIndex);
            button.gameObject.SetActive(true);
            button.interactable = true;
            button.onClick = new Button.ButtonClickedEvent();
            // Seulement « Label » et « Selected/LabelSelected » : « KeyHint/Text » est l'indice de touche.
            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
                if (text.name.Contains("Label"))
                    text.text = title;
            // Indice de touche « Q » (TabLeft) du premier bouton vanilla : Q/E naviguent bien, mais Edia n'en veut pas.
            var hint = button.transform.Find("KeyHint");
            if (hint != null)
                Object.Destroy(hint.gameObject);
            return button;
        }
    }
}
