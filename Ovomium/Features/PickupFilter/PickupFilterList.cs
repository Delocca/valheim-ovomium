using System.Collections.Generic;
using System.Linq;

namespace Ovomium.Features.PickupFilter
{
    /// <summary>
    /// Forme texte de la liste des objets ignorés (valeur de <c>Player.m_customData</c>) : noms d'objet
    /// (<c>$item_…</c>) triés, un par ligne. Logique pure (aucune dépendance Unity/Valheim), testée par
    /// <c>Ovomium.Tests</c>.
    /// </summary>
    internal static class PickupFilterList
    {
        private const char Separator = '\n';

        public static HashSet<string> Parse(string raw)
        {
            var names = new HashSet<string>();
            if (string.IsNullOrEmpty(raw))
                return names;
            foreach (string name in raw.Split(Separator))
            {
                string trimmed = name.Trim();
                if (trimmed.Length > 0)
                    names.Add(trimmed);
            }
            return names;
        }

        /// <summary>Chaîne vide pour une liste vide ; ordre stable (tri ordinal) d'une sauvegarde à l'autre.</summary>
        public static string Serialize(IEnumerable<string> names) =>
            string.Join(Separator.ToString(), names.OrderBy(name => name, System.StringComparer.Ordinal));
    }
}
