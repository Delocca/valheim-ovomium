using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace Ovomium.Loader
{
    /// <summary>
    /// Un cœur démarré : son assembly et l'objet hôte de ses composants (guetteurs), enfant de l'objet du chargeur,
    /// détruit immédiatement à l'arrêt pour qu'aucun composant de l'ancienne version ne cohabite avec la nouvelle.
    /// Contrat avec le cœur, par réflexion (aucune référence entre les deux DLL) :
    /// <c>public static void Ovomium.Plugin.Load(GameObject host, ManualLogSource log, System.Action&lt;string&gt; requestReload)</c>
    /// et <c>public static void Ovomium.Plugin.Unload()</c>.
    /// </summary>
    internal sealed class CoreInstance
    {
        private const string EntryType = "Ovomium.Plugin";

        public readonly CoreAssembly Source;
        private GameObject m_host;
        private MethodInfo m_unload;

        private CoreInstance(CoreAssembly source)
        {
            Source = source;
        }

        /// <summary>Démarre le cœur ; en cas d'échec, défait ce qui a été posé et renvoie null (erreur dans
        /// <paramref name="error"/>).</summary>
        public static CoreInstance Start(CoreAssembly source, Transform parent, System.Action<string> requestReload, out string error)
        {
            var core = new CoreInstance(source);
            try
            {
                Assembly assembly = source.Load();
                System.Type entry = assembly.GetType(EntryType, true);
                MethodInfo load = entry.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
                core.m_unload = entry.GetMethod("Unload", BindingFlags.Public | BindingFlags.Static);
                if (load == null || core.m_unload == null)
                    throw new System.MissingMethodException($"{EntryType}.Load / Unload absents");
                core.m_host = new GameObject("Ovomium.Core");
                core.m_host.transform.SetParent(parent, false);
                load.Invoke(null, new object[] { core.m_host, OvomiumLoader.Log, requestReload });
                error = null;
                return core;
            }
            catch (System.Exception e)
            {
                error = Unwrap(e).ToString();
                core.Stop();
                return null;
            }
        }

        /// <summary>Déchargement complet et synchrone : features, patches, puis composants de l'hôte.</summary>
        public void Stop()
        {
            try { m_unload?.Invoke(null, new object[0]); }
            catch (System.Exception e) { OvomiumLoader.Log.LogError($"Déchargement incomplet : {Unwrap(e)}"); }
            m_unload = null;
            if (m_host != null)
                Object.DestroyImmediate(m_host);
            m_host = null;
        }

        private static System.Exception Unwrap(System.Exception e)
        {
            return e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
        }
    }
}
