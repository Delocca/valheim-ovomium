using System.IO;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ovomium.Loader
{
    /// <summary>
    /// Seul plugin vu par BepInEx : démarre Ovomium.Core.dll (<see cref="CoreInstance"/>) et la remplace à chaud,
    /// toujours de façon synchrone (ancienne version entièrement déchargée avant que la nouvelle démarre), à la
    /// frame qui suit la demande :
    /// - mise à jour : le cœur appelle <c>requestReload(chemin)</c> (Updater, depuis <c>update/</c>) ;
    /// - mode dev (fichier <see cref="DevMarker"/> posé par <c>tools/deploy.sh --dev</c>) : au changement de date
    ///   d'Ovomium.Core.dll (sondage chaque seconde) ou sur F6.
    /// Échec du démarrage de la nouvelle version : l'ancienne (octets gardés en mémoire) est redémarrée.
    /// Le cœur lit le résultat dans l'AppDomain (<see cref="CorePathKey"/>, <see cref="LastErrorKey"/>, chaînes).
    /// </summary>
    [BepInPlugin("ovo.ovomium", "Ovomium", "1.0.0")]
    public sealed class OvomiumLoader : BaseUnityPlugin
    {
        public const string CoreFile = "Ovomium.Core.dll";
        public const string DevMarker = "dev-reload";
        public const string CorePathKey = "Ovomium.Loader.CorePath";
        public const string LastErrorKey = "Ovomium.Loader.LastError";
        private const float DevPollSeconds = 1f;

        internal static ManualLogSource Log;

        private string m_folder;
        private CoreInstance m_current;
        private string m_pending;
        private float m_nextPoll;
        private System.DateTime m_devStamp;

        private string DefaultCorePath => Path.Combine(m_folder, CoreFile);

        private void Awake()
        {
            Log = Logger;
            m_folder = Path.GetDirectoryName(Info.Location);
            m_devStamp = WriteTime(DefaultCorePath);
            Reload(DefaultCorePath);
        }

        private void Update()
        {
            if (m_pending != null)
            {
                string path = m_pending;
                m_pending = null;
                Reload(path);
                return;
            }
            // Input System : KeyboardShortcut.IsDown (entrée legacy) ne voit rien dans Valheim 1.0
            bool key = Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame;
            if (!key && Time.unscaledTime < m_nextPoll)
                return;
            m_nextPoll = Time.unscaledTime + DevPollSeconds;
            if (!File.Exists(Path.Combine(m_folder, DevMarker)))
                return;
            System.DateTime stamp = WriteTime(DefaultCorePath);
            if (!key && stamp == m_devStamp)
                return;
            m_devStamp = stamp;
            Log.LogInfo("Mode dev : rechargement d'Ovomium.Core.dll");
            Reload(DefaultCorePath);
        }

        private void OnDestroy()
        {
            m_current?.Stop();
            m_current = null;
        }

        /// <summary>Appelé par le cœur (thread principal) : le rechargement a lieu à la frame suivante, hors de sa pile.</summary>
        private void RequestReload(string path)
        {
            m_pending = path;
        }

        private void Reload(string path)
        {
            System.AppDomain.CurrentDomain.SetData(LastErrorKey, "");
            CoreAssembly next;
            try { next = CoreAssembly.Read(path); }
            catch (System.Exception e)
            {
                Fail($"{path} illisible : {e.Message}");
                return;  // version en place conservée, rien n'a été déchargé
            }
            CoreAssembly previous = m_current?.Source;
            m_current?.Stop();
            m_current = null;
            if (StartCore(next) || previous == null)
                return;
            if (StartCore(previous))
                Log.LogWarning($"Version précédente redémarrée ({previous.Path})");
        }

        // Pas « Start » : Unity refuse un Start à paramètres sur un MonoBehaviour (erreur au journal)
        private bool StartCore(CoreAssembly source)
        {
            m_current = CoreInstance.Start(source, transform, RequestReload, out string error);
            if (m_current == null)
            {
                Fail($"Chargement de {source.Path} impossible : {error}");
                return false;
            }
            System.AppDomain.CurrentDomain.SetData(CorePathKey, source.Path);
            return true;
        }

        private static void Fail(string error)
        {
            Log.LogError(error);
            System.AppDomain.CurrentDomain.SetData(LastErrorKey, error);
        }

        private static System.DateTime WriteTime(string path)
        {
            return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : System.DateTime.MinValue;
        }
    }
}
