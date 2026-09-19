using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Ovomium
{
    /// <summary>
    /// Autocontrôle au chargement : pose les patches classe par classe (un ancrage disparu n'emporte pas les autres
    /// fonctionnalités) puis force la compilation JIT de tout le mod, pour qu'un membre du jeu disparu (mise à jour)
    /// soit signalé dans le journal au démarrage et non au premier appel en partie. Une ligne go/no-go conclut.
    /// </summary>
    internal static class PatchInstaller
    {
        public static void Install(Harmony harmony)
        {
            var assembly = typeof(PatchInstaller).Assembly;
            var types = AccessTools.GetTypesFromAssembly(assembly);
            int classes = 0, failures = 0;
            foreach (var type in types)
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0) continue;
                classes++;
                try { harmony.CreateClassProcessor(type).Patch(); }
                catch (Exception e)
                {
                    failures++;
                    Plugin.Log.LogError($"Patch {type.FullName} non posé : {Describe(e)}");
                }
            }

            int methods = 0;
            foreach (var type in types)
                failures += CompileMethods(type, ref methods);

            if (failures == 0)
                Plugin.Log.LogInfo($"Autocontrôle OK : {classes} classes de patch posées, {methods} méthodes compilées");
            else
                Plugin.Log.LogError($"Autocontrôle : {failures} échec(s), mise à jour du jeu ? Les fonctionnalités " +
                                    "concernées sont désactivées, les autres tournent");
        }

        /// <summary>Compile chaque méthode déclarée par le type ; retourne le nombre d'échecs.</summary>
        private static int CompileMethods(Type type, ref int compiled)
        {
            if (type.ContainsGenericParameters) return 0;
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                     BindingFlags.Static | BindingFlags.DeclaredOnly;
            int failures = 0;
            foreach (var method in type.GetMethods(all))
            {
                if (method.IsAbstract || method.ContainsGenericParameters) continue;
                try
                {
                    RuntimeHelpers.PrepareMethod(method.MethodHandle);
                    compiled++;
                }
                catch (Exception e)
                {
                    failures++;
                    Plugin.Log.LogError($"{type.FullName}.{method.Name} ne compile pas : {Describe(e)}");
                }
            }
            return failures;
        }

        private static string Describe(Exception e)
        {
            while (e.InnerException != null) e = e.InnerException;
            return $"{e.GetType().Name} : {e.Message}";
        }
    }
}
