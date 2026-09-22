using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Ligne « Ovomium x » y disponible » au menu principal, clone du libellé de version vanilla
    /// (<c>FejdStartup.m_versionLabel</c>) posé juste au-dessus (le vanilla est au bord bas de l'écran, au-dessous
    /// ça sort de la fenêtre). Rien tant qu'il n'y a pas de mise à jour : la version du mod est le titre de la
    /// fenêtre Ovomium des Paramètres.
    /// </summary>
    internal static class UpdaterMenuLabel
    {
        /// <summary>Nom du GameObject cloné : seule clé fiable au rechargement à chaud.</summary>
        public const string LabelName = "OvomiumUpdateLabel";
        private const string Highlight = "#E8D5A8";

        /// <summary>Texte selon l'état courant, vide si rien à montrer.</summary>
        public static string CurrentText()
        {
            if (UpdateState.Downloaded)
                return $"Ovomium <color={Highlight}>{UpdateState.Version}</color> téléchargée : installée au prochain lancement";
            if (UpdateState.Downloading)
                return $"Ovomium <color={Highlight}>{UpdateState.Version}</color> : téléchargement…"
                    + (UpdateState.RelaunchRequested ? " le jeu va se relancer" : "");
            if (UpdateState.Available)  // « → » absent de la police du jeu (remplacé par une espace)
                return $"Ovomium {PluginVersion.Value} » <color={Highlight}>{UpdateState.Version}</color> disponible";
            return "";
        }

        /// <summary>Pose ou met à jour la ligne (thread principal).</summary>
        public static void Apply(FejdStartup startup, string text)
        {
            if (startup == null || startup.m_versionLabel == null)
                return;
            TMP_Text label = Find(startup.m_versionLabel.transform.parent) ?? Clone(startup.m_versionLabel);
            label.text = text;
            label.gameObject.SetActive(text.Length > 0);
        }

        internal static void Unload()
        {
            if (FejdStartup.instance == null || FejdStartup.instance.m_versionLabel == null)
                return;
            var doomed = new List<GameObject>();
            foreach (var t in FejdStartup.instance.m_versionLabel.transform.parent.GetComponentsInChildren<Transform>(true))
                if (t.name == LabelName)
                    doomed.Add(t.gameObject);
            foreach (var go in doomed)
                Object.DestroyImmediate(go);  // immédiat : le nouveau plugin repose le sien dans la même frame
        }

        private static TMP_Text Find(Transform parent)
        {
            foreach (var text in parent.GetComponentsInChildren<TMP_Text>(true))
                if (text.name == LabelName)
                    return text;
            return null;
        }

        private static TMP_Text Clone(TMP_Text template)
        {
            TMP_Text label = Object.Instantiate(template, template.transform.parent);
            label.name = LabelName;
            label.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            label.richText = true;
            var rect = label.GetComponent<RectTransform>();
            var templateRect = template.GetComponent<RectTransform>();
            if (rect != null && templateRect != null)  // même pivot et ancres que le modèle : + hauteur = juste au-dessus
                rect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, templateRect.rect.height);
            return label;
        }
    }
}
