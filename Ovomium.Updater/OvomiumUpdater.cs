using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;

namespace Ovomium.Updater
{
    /// <summary>
    /// Patcher BepInEx : au lancement du jeu, avant tout chargement de plugin, copie le contenu de
    /// <c>BepInEx/plugins/Ovomium/update/</c> (déposé par la feature Updater du mod) par-dessus le dossier du plugin,
    /// puis le supprime. Windows verrouille une DLL chargée : c'est le seul moment où Ovomium.dll est remplaçable.
    /// Ne patche aucune assembly : <c>TargetDLLs</c> est le premier membre appelé par BepInEx 5, l'installation s'y
    /// fait, et la liste renvoyée est vide.
    /// </summary>
    public static class OvomiumUpdater
    {
        private const string PluginFolder = "Ovomium";
        private const string UpdateFolder = "update";
        private static bool s_done;

        public static IEnumerable<string> TargetDLLs
        {
            get
            {
                if (!s_done)
                {
                    s_done = true;
                    InstallPending();
                }
                yield break;
            }
        }

        public static void Patch(Mono.Cecil.AssemblyDefinition assembly)
        {
        }

        private static void InstallPending()
        {
            ManualLogSource log = Logger.CreateLogSource("Ovomium.Updater");
            try
            {
                if (string.IsNullOrEmpty(Paths.PluginPath))
                    return;
                string plugin = Path.Combine(Paths.PluginPath, PluginFolder);
                string update = Path.Combine(plugin, UpdateFolder);
                if (!Directory.Exists(update))
                    return;
                int count = CopyTree(update, plugin);
                Directory.Delete(update, true);
                log.LogInfo($"Ovomium {ReadVersion(plugin)} installée ({count} fichier(s))");
            }
            catch (System.Exception e)
            {
                log.LogError($"Installation de la mise à jour échouée (la version en place reste utilisée) : {e}");
            }
        }

        /// <summary>Copie récursive avec écrasement ; renvoie le nombre de fichiers copiés.</summary>
        private static int CopyTree(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            int count = 0;
            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
                count++;
            }
            foreach (string dir in Directory.GetDirectories(source))
                count += CopyTree(dir, Path.Combine(destination, Path.GetFileName(dir)));
            return count;
        }

        private static string ReadVersion(string plugin)
        {
            string file = Path.Combine(plugin, "version.txt");
            return File.Exists(file) ? File.ReadAllText(file).Trim() : "(version inconnue)";
        }
    }
}
