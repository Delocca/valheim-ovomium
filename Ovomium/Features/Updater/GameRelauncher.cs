using System.Diagnostics;
using System.IO;
using System.Text;
using Ovomium.Features.AutoJoin;
using UnityEngine;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Relance du jeu après téléchargement : un script détaché attend la fin du processus courant puis relance
    /// l'exécutable avec ses arguments d'origine (sans <c>-ovomium-autojoin</c> : retour au menu principal), en
    /// héritant de l'environnement Steam et du répertoire courant. Pas de <c>steam://</c> : sous Linux le reaper de
    /// Steam attend la fin de tous les descendants et ignorerait la relance tant que le jeu est « en cours » ;
    /// relancer l'exécutable depuis un descendant garde une session Steam continue. Sous Linux, l'environnement
    /// hérité contient déjà <c>LD_PRELOAD</c> et les <c>DOORSTOP_*</c> ; seul <c>DOORSTOP_INITIALIZED=TRUE</c>, posé par
    /// Doorstop pour ne pas s'injecter dans les processus enfants, doit être retiré (sinon le jeu relancé n'a pas
    /// BepInEx). Sous Windows le proxy <c>winhttp.dll</c> se charge sans environnement.
    /// </summary>
    internal static class GameRelauncher
    {
        /// <summary>Écrit et lance le script de relance ; vrai s'il tourne (le jeu peut alors quitter).</summary>
        public static bool Start()
        {
            try
            {
                bool windows = Application.platform == RuntimePlatform.WindowsPlayer;
                string[] args = System.Environment.GetCommandLineArgs();
                int pid = Process.GetCurrentProcess().Id;
                string script = Path.Combine(UpdateChecker.PluginFolder, windows ? "relaunch.bat" : "relaunch.sh");
                File.WriteAllText(script, windows ? BatchScript(pid, args) : ShellScript(pid, args));
                Process.Start(StartInfo(windows, script));
                Plugin.Log.LogInfo($"Updater : script de relance lancé ({script}), en attente de la fin du processus {pid}");
                return true;
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"Updater : relance du jeu impossible : {e.Message}");
                return false;
            }
        }

        private static ProcessStartInfo StartInfo(bool windows, string script)
        {
            var info = windows
                ? new ProcessStartInfo("cmd.exe", $"/c \"{script}\"")
                : new ProcessStartInfo("/bin/sh", ShellQuote(script));
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            info.WorkingDirectory = System.Environment.CurrentDirectory;
            return info;
        }

        private static string ShellScript(int pid, string[] args)
        {
            var sb = new StringBuilder("#!/bin/sh\n");
            sb.Append($"while kill -0 {pid} 2>/dev/null; do sleep 1; done\n");
            sb.Append("unset DOORSTOP_INITIALIZED\n");
            sb.Append("exec");
            foreach (string arg in Relaunched(args))
                sb.Append(' ').Append(ShellQuote(arg));
            return sb.Append('\n').ToString();
        }

        private static string BatchScript(int pid, string[] args)
        {
            var sb = new StringBuilder("@echo off\r\n:wait\r\n");
            sb.Append($"tasklist /FI \"PID eq {pid}\" /NH 2>nul | findstr /R /C:\" {pid} \" >nul && (ping -n 2 127.0.0.1 >nul & goto wait)\r\n");
            sb.Append("set DOORSTOP_INITIALIZED=\r\nstart \"\"");
            foreach (string arg in Relaunched(args))
                sb.Append(" \"").Append(arg).Append('"');
            return sb.Append("\r\n").ToString();
        }

        /// <summary>Exécutable (chemin absolu : un nom nu serait cherché dans le PATH) puis arguments d'origine, sans
        /// le drapeau AutoJoin.</summary>
        private static System.Collections.Generic.IEnumerable<string> Relaunched(string[] args)
        {
            yield return Path.GetFullPath(args[0]);
            for (int i = 1; i < args.Length; i++)
                if (args[i] != AutoJoinConfig.CommandLineFlag)
                    yield return args[i];
        }

        /// <summary>Guillemets simples POSIX : rien n'y est interprété.</summary>
        private static string ShellQuote(string value)
        {
            return "'" + value.Replace("'", "'\\''") + "'";
        }
    }
}
