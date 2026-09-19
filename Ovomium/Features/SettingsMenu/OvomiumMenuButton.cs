using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>Bouton « Ovomium » cloné du bouton Paramètres, juste après lui, dans le menu Échap et le menu principal.</summary>
    internal static class OvomiumMenuButton
    {
        private const string Title = "Ovomium";
        /// <summary>Nom du GameObject cloné : seule clé fiable au rechargement à chaud (les types du mod changent d'assembly).</summary>
        public const string ButtonName = "OvomiumMenuButton";

        /// <summary>
        /// Rechargement à chaud : pose le bouton dans les menus déjà en scène (les patches de <c>Start</c> ne rejoueront
        /// pas). Ferme d'abord une fenêtre Ovomium laissée ouverte par l'ancienne version.
        /// </summary>
        public static void Install()
        {
            if (!SettingsMenuConfig.Enabled.Value)
                return;
            OvomiumSettingsWindow.CloseAll();
            if (Menu.instance != null)
                AddToMenu(Menu.instance);
            if (FejdStartup.instance != null)
                AddToMainMenu(FejdStartup.instance);
        }

        /// <summary>Déchargement du plugin (rechargement à chaud) : ferme la fenêtre Ovomium puis retire les boutons.</summary>
        internal static void Unload()
        {
            OvomiumSettingsWindow.CloseAll();
            RemoveAll();
        }

        /// <summary>Retire nos boutons des deux menus, leurs listeners visent le code de l'assembly qui meurt.</summary>
        public static void RemoveAll()
        {
            if (Menu.instance != null && Menu.instance.m_settingsButton != null)
                DestroyExisting(Menu.instance.m_settingsButton.transform.parent);
            if (FejdStartup.instance != null && FejdStartup.instance.m_menuList != null)
                DestroyExisting(FejdStartup.instance.m_menuList.transform);
        }

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

        /// <summary>Un clone existant (ancienne version du mod) est détruit, jamais réutilisé : recréer est idempotent.</summary>
        private static void Clone(Button template, UnityEngine.Events.UnityAction onClick)
        {
            DestroyExisting(template.transform.parent);
            var button = Object.Instantiate(template, template.transform.parent);
            button.name = ButtonName;
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

        /// <summary>Détruit tout descendant nommé <see cref="ButtonName"/> (inactifs compris : le menu Échap est caché).</summary>
        private static void DestroyExisting(Transform root)
        {
            var doomed = new List<GameObject>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t != root && t.name == ButtonName)
                    doomed.Add(t.gameObject);
            foreach (var go in doomed)
                Object.Destroy(go);
        }
    }
}
