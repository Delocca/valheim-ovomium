using System.Collections;
using System.Collections.Generic;
using Ovomium.Features.AutoJoin;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ovomium.Features.ContinueButton
{
    /// <summary>
    /// Bouton « Continuer : &lt;partie&gt; » cloné du bouton Démarrer du menu principal, posé juste avant lui. Absent si
    /// aucune partie n'est mémorisée. Serveur : chemin AutoJoin (mot de passe mémorisé soumis) ; monde local :
    /// OnCharacterStart() (sélection du personnage + panneau des mondes, qui restaure le dernier monde) puis
    /// OnWorldStart() dès que le bouton Démarrer vanilla est actif (son état est posé par FejdStartup.Update).
    /// </summary>
    internal static class ContinueMenuButton
    {
        /// <summary>Nom du GameObject cloné : seule clé fiable au rechargement à chaud.</summary>
        public const string ButtonName = "OvomiumContinueButton";
        private const int MaxNameLength = 22;
        private const float StartTimeoutSeconds = 2f;

        /// <summary>Rechargement à chaud : le menu est déjà en scène, son Start ne rejouera pas.</summary>
        public static void Install()
        {
            if (!ContinueButtonConfig.Enabled.Value)
                return;
            LastSession.RememberCurrent();
            if (FejdStartup.instance != null)
                AddToMainMenu(FejdStartup.instance);
        }

        /// <summary>Déchargement du plugin : le listener du bouton vise l'assembly qui meurt.</summary>
        internal static void Unload()
        {
            if (FejdStartup.instance != null && FejdStartup.instance.m_menuList != null)
                DestroyExisting(FejdStartup.instance.m_menuList.transform);
        }

        public static void AddToMainMenu(FejdStartup startup)
        {
            var startButton = FindStartButton(startup);
            if (startButton == null)
            {
                Plugin.Log.LogWarning("ContinueButton : bouton Démarrer introuvable dans FejdStartup.m_menuList");
                return;
            }
            DestroyExisting(startButton.transform.parent);
            LastSession session = LastSession.Find();
            if (session == null)
            {
                Plugin.Log.LogInfo("ContinueButton : aucune partie à continuer, pas de bouton");
                return;
            }
            Clone(startButton, Label(session), () => Continue(startup, session));
        }

        /// <summary>Le type de partie en petit et atténué : des icônes du jeu ont été essayées, jugées moins parlantes.</summary>
        private static string Label(LastSession session)
        {
            string name = session.Name.Length > MaxNameLength ? session.Name.Substring(0, MaxNameLength - 1) + "…" : session.Name;
            string kind = session.IsServer ? "serveur" : "local";
            return $"Continuer : <color=#E8D5A8>{name}</color> <size=65%><color=#FFFFFF99>{kind}</color></size>";
        }

        private static void Continue(FejdStartup startup, LastSession session)
        {
            if (startup.m_profiles == null || startup.m_profileIndex < 0 || startup.m_profileIndex >= startup.m_profiles.Count)
            {
                Plugin.Log.LogInfo("ContinueButton : aucun personnage sélectionné par défaut, menu vanilla");
                startup.OnStartGame();
                return;
            }
            if (session.IsServer)
            {
                AutoJoinPatch.Join(startup, startup.m_profileIndex, session.Server, "ContinueButton");
                return;
            }
            Plugin.Log.LogInfo($"ContinueButton : monde local « {session.Name} », personnage "
                + $"{startup.m_profiles[startup.m_profileIndex].GetName()}");
            startup.m_instantStart = true;
            startup.OnCharacterStart();
            startup.StartCoroutine(StartWorldWhenReady(startup, session.World));
        }

        /// <summary>Le panneau des mondes reste affiché (chemin vanilla) si le monde restauré n'est pas démarrable.</summary>
        private static IEnumerator StartWorldWhenReady(FejdStartup startup, World world)
        {
            float deadline = Time.unscaledTime + StartTimeoutSeconds;
            while (Time.unscaledTime < deadline)
            {
                yield return null;
                if (startup.m_world == null || startup.m_world.m_name != world.m_name || !startup.m_startGamePanel.activeInHierarchy)
                    continue;
                if (startup.m_worldStart != null && startup.m_worldStart.interactable)
                {
                    startup.OnWorldStart();
                    Plugin.Log.LogInfo("ContinueButton : monde démarré");
                    yield break;
                }
            }
            Plugin.Log.LogInfo("ContinueButton : le monde n'est pas démarrable, panneau des mondes laissé ouvert");
        }

        /// <summary>Le bouton dont un listener persistant (câblé dans le prefab) appelle <c>OnStartGame</c>.</summary>
        private static Button FindStartButton(FejdStartup startup)
        {
            if (startup.m_menuList == null)
                return null;
            foreach (var button in startup.m_menuList.GetComponentsInChildren<Button>(true))
                for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    if (button.onClick.GetPersistentMethodName(i) == nameof(FejdStartup.OnStartGame))
                        return button;
            return null;
        }

        private static void Clone(Button template, string label, UnityEngine.Events.UnityAction onClick)
        {
            var button = Object.Instantiate(template, template.transform.parent);
            button.name = ButtonName;
            button.transform.SetSiblingIndex(template.transform.GetSiblingIndex());
            button.gameObject.SetActive(true);
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(onClick);
            var texts = button.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length == 0)
                Plugin.Log.LogWarning($"ContinueButton : aucun TMP_Text dans le bouton « {template.name} », libellé non posé");
            foreach (var text in texts)
                text.text = label;
        }

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
