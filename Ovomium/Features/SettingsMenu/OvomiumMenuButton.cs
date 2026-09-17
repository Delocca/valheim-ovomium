using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>Bouton « Ovomium » cloné du bouton Paramètres, juste après lui, dans le menu Échap et le menu principal.</summary>
    internal static class OvomiumMenuButton
    {
        private const string Title = "Ovomium";

        public static void AddToMenu(Menu menu)
        {
            if (menu.m_settingsButton == null)
            {
                Plugin.Log.LogWarning("SettingsMenu : Menu.m_settingsButton absent, pas de bouton Ovomium dans le menu Échap");
                return;
            }
            Clone(menu.m_settingsButton, () => OvomiumSettingsWindow.OpenFromMenu(menu));
        }

        public static void AddToMainMenu(FejdStartup startup)
        {
            var settingsButton = FindSettingsButton(startup);
            if (settingsButton == null)
            {
                Plugin.Log.LogWarning("SettingsMenu : bouton Paramètres introuvable dans FejdStartup.m_menuList");
                return;
            }
            Clone(settingsButton, () => OvomiumSettingsWindow.OpenFromMainMenu(startup));
        }

        /// <summary>Le bouton dont un listener persistant (câblé dans le prefab) appelle <c>OnButtonSettings</c>.</summary>
        private static Button FindSettingsButton(FejdStartup startup)
        {
            if (startup.m_menuList == null)
                return null;
            foreach (var button in startup.m_menuList.GetComponentsInChildren<Button>(true))
                for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    if (button.onClick.GetPersistentMethodName(i) == nameof(FejdStartup.OnButtonSettings))
                        return button;
            return null;
        }

        private static void Clone(Button template, UnityEngine.Events.UnityAction onClick)
        {
            var button = Object.Instantiate(template, template.transform.parent);
            button.name = "Ovomium";
            button.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            button.gameObject.SetActive(true);
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(onClick);
            var texts = button.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length == 0)
                Plugin.Log.LogWarning($"SettingsMenu : aucun TMP_Text dans le bouton « {template.name} », libellé non posé");
            foreach (var text in texts)
                text.text = Title;
        }
    }
}
