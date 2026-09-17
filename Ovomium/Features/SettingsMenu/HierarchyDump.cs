using System.Text;
using UnityEngine;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary>Écrit dans le journal la hiérarchie d'un objet (nom, composants), pour diagnostiquer un prefab inconnu.</summary>
    internal static class HierarchyDump
    {
        public static void Log(Transform root, int maxDepth = 5)
        {
            var sb = new StringBuilder();
            sb.Append("Hiérarchie de ").Append(root.name).AppendLine(" :");
            Append(sb, root, 0, maxDepth);
            Plugin.Log.LogInfo(sb.ToString());
        }

        private static void Append(StringBuilder sb, Transform t, int depth, int maxDepth)
        {
            sb.Append(' ', depth * 2).Append(t.name);
            if (!t.gameObject.activeSelf)
                sb.Append(" (inactif)");
            sb.Append(" [");
            var first = true;
            foreach (var c in t.GetComponents<Component>())
            {
                if (c == null || c is Transform)
                    continue;
                if (!first)
                    sb.Append(", ");
                sb.Append(c.GetType().Name);
                first = false;
            }
            sb.AppendLine("]");
            if (depth >= maxDepth)
                return;
            for (var i = 0; i < t.childCount; i++)
                Append(sb, t.GetChild(i), depth + 1, maxDepth);
        }
    }
}
